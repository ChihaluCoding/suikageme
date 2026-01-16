using System;
using UnityEngine;

public class GameOverButtonHandler : MonoBehaviour
{
    private Action onClick;

    public void Init(Action action)
    {
        onClick = action;
    }

    private void OnMouseDown()
    {
        onClick?.Invoke();
    }
}
