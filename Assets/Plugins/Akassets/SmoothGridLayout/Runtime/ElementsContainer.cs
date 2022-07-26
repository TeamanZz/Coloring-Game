using System;
using UnityEngine;

namespace Akassets.SmoothGridLayout
{
    public class ElementsContainer : MonoBehaviour
    {
        public Action OnChildrenChanged;
    
        private void OnTransformChildrenChanged()
        {
            OnChildrenChanged?.Invoke();
        }

        private void OnRectTransformDimensionsChange()
        {
            OnChildrenChanged?.Invoke();
        }
    }
}
