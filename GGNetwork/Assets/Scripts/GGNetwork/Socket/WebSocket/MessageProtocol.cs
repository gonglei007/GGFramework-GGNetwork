using System;
using System.Collections.Generic;
using System.Text;
using SimpleJson;

namespace GGFramework.GGNetwork.Socket
{
    /// <summary>
    /// 消息编解码（Pomelo 应用层消息协议）。
    ///
    /// 消息头：
    ///   [flag(1)] [id?] [route?]
    ///   flag bit0 = route 是否压缩（用 dict）；bit1-3 = 消息类型
    ///   id 为 varint（仅请求/响应）
    ///   route：若压缩用 2 字节索引；否则 1 字节长度 + UTF-8 字符串
    ///
    /// 消息体（body）：
    ///   本项目内部实现统一使用 JSON（SimpleJson）编码。服务端若开启 protobuf
    ///   压缩，则需在握手 sys.protos 中下发 schema 并在此扩展 protobuf 编解码分支。
    /// </summary>
    public class MessageProtocol
    {
        public const int MSG_Route_Limit = 255;
        public const int MSG_Route_Mask = 0x01;
        public const int MSG_Type_Mask = 0x07;

        private readonly Dictionary<string, ushort> dict = new Dictionary<string, ushort>();
        private readonly Dictionary<ushort, string> abbrs = new Dictionary<ushort, string>();
        private readonly Dictionary<uint, string> reqMap = new Dictionary<uint, string>();
        private readonly object reqMapLock = new object();

        public MessageProtocol(JsonObject dict)
        {
            if (dict != null && dict.Count > 0)
            {
                foreach (string key in dict.Keys)
                {
                    ushort value = Convert.ToUInt16(dict[key]);
                    this.dict[key] = value;
                    this.abbrs[value] = key;
                }
            }
        }

        /// <summary>编码请求/通知消息。</summary>
        public byte[] Encode(string route, uint id, JsonObject msg)
        {
            int routeLength = ByteLength(route);
            if (routeLength > MSG_Route_Limit)
            {
                throw new Exception("Route is too long! - " + route);
            }

            // 最大头长：flag(1) + id varint(至多5) + route(1+255)
            byte[] head = new byte[routeLength + 8];
            int offset = 1;
            byte flag = 0;

            if (id > 0)
            {
                byte[] bytes = WsEncoder.EncodeUInt32(id);
                WriteBytes(bytes, offset, head);
                flag |= ((byte)MessageType.MSG_REQUEST) << 1;
                offset += bytes.Length;
            }
            else
            {
                flag |= ((byte)MessageType.MSG_NOTIFY) << 1;
            }

            // 路由压缩
            if (dict.ContainsKey(route))
            {
                ushort cmpRoute = dict[route];
                WriteShort(offset, cmpRoute, head);
                flag |= MSG_Route_Mask;
                offset += 2;
            }
            else
            {
                head[offset++] = (byte)routeLength;
                WriteBytes(Encoding.UTF8.GetBytes(route), offset, head);
                offset += routeLength;
            }

            head[0] = flag;

            // body：本项目统一用 JSON
            byte[] body = Encoding.UTF8.GetBytes(msg != null ? msg.ToString() : "{}");

            byte[] result = new byte[offset + body.Length];
            for (int i = 0; i < offset; i++) result[i] = head[i];
            for (int i = 0; i < body.Length; i++) result[offset + i] = body[i];

            // 记录 reqId -> route 映射（用于响应回填 route）
            if (id > 0)
            {
                lock (reqMapLock) reqMap[id] = route;
            }
            return result;
        }

        /// <summary>解码单个消息（从包体）。</summary>
        public Message Decode(byte[] buffer)
        {
            if (buffer == null || buffer.Length < 1) return null;

            byte flag = buffer[0];
            int offset = 1;
            MessageType type = (MessageType)((flag >> 1) & MSG_Type_Mask);
            uint id = 0;
            string route;

            if (type == MessageType.MSG_RESPONSE)
            {
                int length;
                id = WsDecoder.DecodeUInt32(offset, buffer, out length);
                lock (reqMapLock)
                {
                    if (id <= 0 || !reqMap.ContainsKey(id))
                    {
                        // 无法匹配的响应，忽略。
                        return null;
                    }
                    route = reqMap[id];
                    reqMap.Remove(id);
                }
                offset += length;
            }
            else if (type == MessageType.MSG_PUSH)
            {
                if ((flag & 0x01) == 1)
                {
                    ushort routeId = ReadShort(offset, buffer);
                    if (!abbrs.ContainsKey(routeId)) return null;
                    route = abbrs[routeId];
                    offset += 2;
                }
                else
                {
                    byte length = buffer[offset];
                    offset += 1;
                    if (offset + length > buffer.Length) return null;
                    route = Encoding.UTF8.GetString(buffer, offset, length);
                    offset += length;
                }
            }
            else
            {
                // 请求/通知非客户端接收类型，暂不处理。
                return null;
            }

            // 解码 body（JSON）
            int bodyLength = buffer.Length - offset;
            JsonObject msg;
            byte[] body = new byte[bodyLength];
            for (int i = 0; i < bodyLength; i++) body[i] = buffer[offset + i];

            try
            {
                msg = (JsonObject)SimpleJson.SimpleJson.DeserializeObject(Encoding.UTF8.GetString(body));
            }
            catch (Exception e)
            {
                msg = new JsonObject();
                msg["code"] = 500;
                msg["msg"] = "Param json parse error!-" + e.Message;
            }

            return new Message(type, id, route, msg);
        }

        private static void WriteShort(int offset, ushort value, byte[] bytes)
        {
            bytes[offset] = (byte)(value >> 8 & 0xff);
            bytes[offset + 1] = (byte)(value & 0xff);
        }

        private static ushort ReadShort(int offset, byte[] bytes)
        {
            ushort result = 0;
            result += (ushort)(bytes[offset] << 8);
            result += (ushort)(bytes[offset + 1]);
            return result;
        }

        private static int ByteLength(string msg)
        {
            return Encoding.UTF8.GetBytes(msg).Length;
        }

        private static void WriteBytes(byte[] source, int offset, byte[] target)
        {
            for (int i = 0; i < source.Length; i++)
            {
                target[offset + i] = source[i];
            }
        }
    }
}
