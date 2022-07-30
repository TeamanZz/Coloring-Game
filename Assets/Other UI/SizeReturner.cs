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
    public Vector3 saveScale = Vector3.one;

    public void SaveRectTransform()
    {
        saveScale = Vector3.one;
        groupRectTransform.localScale += (Vector3.one * 0.05f);
        isSaved = true;
    }

    [ContextMenu("Return Transform")]
    public void ReturnRectTransfrom()
    {
        groupRectTransform.anchoredPosition = Vector3.zero;
        groupRectTransform.localScale = Vector3.one;

        isSaved = false;
    }

    public void ClearRectTransform()
    {
        saveScale = Vector3.one;
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
