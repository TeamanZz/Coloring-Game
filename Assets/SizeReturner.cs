using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SizeReturner : MonoBehaviour
{
    public Button returnButton;
    public bool isSaved = false;

    [Header("Rect Settings")]
    public RectTransform groupRectTransform;
    public Vector2 saveTransform;
    public Vector2 saveScale;

    public void SaveRectTransform()
    {
        saveTransform = groupRectTransform.anchoredPosition;
        saveScale = groupRectTransform.localScale;
        groupRectTransform.localScale += (Vector3.one * 0.05f);
        isSaved = true;
    }

    [ContextMenu("Return Transform")]
    public void ReturnRectTransfrom()
    {
        groupRectTransform.anchoredPosition = saveTransform;
        groupRectTransform.localScale = saveScale;
        isSaved = false;
    }

    public void ClearRectTransform()
    {
        saveTransform = Vector2.zero;
        saveScale = Vector2.zero;
        isSaved = false;
    }

    public void Update()
    {
        if (isSaved == false)
        {
            returnButton.gameObject.SetActive(false);
            return;
        }
        else
        {
            if (groupRectTransform.localScale.x <= saveScale.x && groupRectTransform.localScale.y <= saveScale.y)
            {
                Debug.Log($"Clear: {groupRectTransform.localScale} and {saveScale}");
                ClearRectTransform();
            }
            else
                returnButton.gameObject.SetActive(true);
        }

    }
}
