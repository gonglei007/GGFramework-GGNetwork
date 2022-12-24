using System;
using UnityEngine;

namespace GGFramework.GGNetwork
{
    public class HTTPForm
    {
        WWWForm form = null;

        public HTTPForm()
        {
            form = new WWWForm();
        }

        public HTTPForm(WWWForm form)
        {
            this.form = form;
        }

        public WWWForm GetUnityForm()
        {
            return this.form;
        }
    }
}
