using System;
using System.Collections;
using UnityEngine;

public class GameOverButtonHandler : MonoBehaviour
{
    private static GameOverButtonHandler animOwner;
    private static int lastClickFrame = -1;

    private Action onClick;
    private Collider2D targetCollider;
    private Camera mainCamera;
    private Vector3 baseScale = Vector3.one;
    private bool animating;

    public void Init(Action action)
    {
        onClick = action;
        targetCollider = GetComponent<Collider2D>();
        mainCamera = Camera.main;
        baseScale = transform.localScale;
    }

    private void OnMouseDown()
    {
        TriggerClick();
    }

    private void Update()
    {
        if (onClick == null || targetCollider == null)
        {
            return;
        }

        if (Input.GetMouseButtonDown(0) && IsPointerOverButton(Input.mousePosition))
        {
            TriggerClick();
            return;
        }

        if (Input.touchCount > 0 && !animating)
        {
            Touch touch = Input.GetTouch(0);
            if (touch.phase == TouchPhase.Began && IsPointerOverButton(touch.position))
            {
                TriggerClick();
            }
        }
    }

    private void TriggerClick()
    {
        if (animating)
        {
            return;
        }
        if (animOwner != null)
        {
            return;
        }
        if (lastClickFrame == Time.frameCount)
        {
            return;
        }
        lastClickFrame = Time.frameCount;
        StartCoroutine(PlayClickAnimation());
    }

    private IEnumerator PlayClickAnimation()
    {
        animating = true;
        animOwner = this;
        Vector3 shrunken = baseScale * 0.9f;
        transform.localScale = shrunken;
        yield return new WaitForSecondsRealtime(0.08f);
        transform.localScale = baseScale;
        onClick?.Invoke();
        animating = false;
        if (animOwner == this)
        {
            animOwner = null;
        }
    }

    private void OnDisable()
    {
        animating = false;
        transform.localScale = baseScale;
        if (animOwner == this)
        {
            animOwner = null;
        }
    }

    private bool IsPointerOverButton(Vector3 screenPosition)
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
            if (mainCamera == null)
            {
                return false;
            }
        }

        Vector3 world = mainCamera.ScreenToWorldPoint(screenPosition);
        return targetCollider.OverlapPoint(new Vector2(world.x, world.y));
    }
}
