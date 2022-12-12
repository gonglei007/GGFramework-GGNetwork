using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class TestingQuestionDialog : MonoBehaviour {


    private void Update() {
        if (Input.GetKeyDown(KeyCode.Space)) {
            QuestionDialogUI.Instance.ShowQuestion("Are you sure you want to quit the game?", () => {
                QuestionDialogUI.Instance.ShowQuestion("Are you really sure?", () => {
                    Application.Quit();
                    EditorApplication.ExitPlaymode();
                }, () => {
                     // Do nothing
                });
            }, () => {
                // Do nothing on No
            });
        }
    }

}