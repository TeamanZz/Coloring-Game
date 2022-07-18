using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;

namespace BizzyBeeGames.PictureColoring
{
    public class PictureScrollArea : MonoBehaviour
    {
        #region Classes

        private class Pointer
        {
            public int id;
            public float time;
            public Vector2 position;
        }

        #endregion

        #region Enums

        private enum State
        {
            None,
            Click,
            Drag,
            Pinch,
            Move
        }

        #endregion

        #region Inspector Variables

        [SerializeField] private GameObject content = null;
        [SerializeField] private float minZoom = 1;
        [SerializeField] private float maxZoom = 2;
        [SerializeField] private float zoomScale = 0.01f;
        [SerializeField] private float mouseWheelZoomScale = 0.03f;
        [SerializeField] private float delayTillDrag = 1;
        [SerializeField] private float longPressTime = 1f;

        #endregion

        #region Member Variables

        private State state;
        private List<Pointer> pointers;
        private Vector2 moveOffset;
        private float lastPinchDist;
        private float timeTillDrag;
        private bool hasFirstDragHappened;

        private bool isAnimating;
        private Vector3 fromValues;
        private Vector3 toValues;
        private float animTimer;
        private float animDuration;

        #endregion

        #region Properties

        public System.Action<Vector2> OnClick { get; set; }
        public System.Action<Vector2> OnLongPress { get; set; }
        public System.Action<Vector2> OnZoom { get; set; }
        public System.Action<Vector2> OnMove { get; set; }
        public System.Action<Vector2> OnDragStart { get; set; }
        public System.Action<Vector2> OnDragged { get; set; }
        public System.Action OnDragEnd { get; set; }

        public RectTransform Content { get { return content.transform as RectTransform; } }
        public RectTransform RectT { get { return transform as RectTransform; } }
        public float MinZoom { get { return minZoom; } set { minZoom = value; } }
        public float MaxZoom { get { return maxZoom; } set { maxZoom = value; } }
        public float CurrentZoom { get { return content.transform.localScale.x; } set { content.transform.localScale = new Vector3(value, value, 1f); } }
        public bool PowerUpMode { get; set; }
        public bool DisableTouch { get; set; }

        #endregion

        #region Unity Methods

        private void Start()
        {
            pointers = new List<Pointer>();

            EventTrigger eventTrigger = gameObject.GetComponent<EventTrigger>();

            if (eventTrigger == null)
            {
                eventTrigger = gameObject.AddComponent<EventTrigger>();
            }

            EventTrigger.Entry pointerDownEntry = new EventTrigger.Entry();
            pointerDownEntry.eventID = EventTriggerType.PointerDown;
            pointerDownEntry.callback.AddListener(OnPointerDown);

            EventTrigger.Entry pointerUpEntry = new EventTrigger.Entry();
            pointerUpEntry.eventID = EventTriggerType.PointerUp;
            pointerUpEntry.callback.AddListener(OnPointerUp);

            EventTrigger.Entry beginDragEntry = new EventTrigger.Entry();
            beginDragEntry.eventID = EventTriggerType.BeginDrag;
            beginDragEntry.callback.AddListener(OnBeginDrag);

            EventTrigger.Entry dragEntry = new EventTrigger.Entry();
            dragEntry.eventID = EventTriggerType.Drag;
            dragEntry.callback.AddListener(OnDrag);

            eventTrigger.triggers.Add(pointerDownEntry);
            eventTrigger.triggers.Add(pointerUpEntry);
            eventTrigger.triggers.Add(beginDragEntry);
            eventTrigger.triggers.Add(dragEntry);
        }

        private void Update()
        {
            if (isAnimating)
            {
                animTimer += Time.deltaTime;

                if (animTimer >= animDuration)
                {
                    isAnimating = false;
                    animTimer = animDuration;
                }

                float t = Utilities.EaseOut(animTimer / animDuration);
                float x = Mathf.Lerp(fromValues[0], toValues[0], t);
                float y = Mathf.Lerp(fromValues[1], toValues[1], t);
                float scale = Mathf.Lerp(fromValues[2], toValues[2], t);

                (content.transform as RectTransform).anchoredPosition = new Vector2(x, y);
                content.transform.localScale = new Vector3(scale, scale, 1f);

                KeepInScrollArea();

                if (OnZoom != null)
                {
                    OnZoom(content.transform.localScale);
                }

                return;
            }

            if (DisableTouch)
            {
                return;
            }

            if (delayTillDrag != 0 && state == State.Click)
            {
                timeTillDrag -= Time.deltaTime;

                if (timeTillDrag <= 0)
                {
                    SetState(State.Drag);

                    hasFirstDragHappened = false;

                    if (OnDragStart != null)
                    {
                        OnDragStart(pointers[0].position);
                    }
                }
            }
            else if (state == State.Drag && !hasFirstDragHappened)
            {
                if(OnDragged != null)
                    OnDragged(Utilities.MousePosition());
            }

            for (int i = 0; i < pointers.Count; i++)
            {
                Pointer pointer = pointers[i];
                pointer.time += Time.deltaTime;

                if(pointer.time >= longPressTime)
                    OnLongPress(pointer.position);
            }

#if UNITY_STANDALONE || UNITY_EDITOR

            UpdateMouseWheel();

#endif
        }

        #endregion

        #region Public Methods

        public void ResetObj()
        {
            isAnimating = false;

            content.transform.localScale = new Vector3(1f, 1f, 1f);
            (content.transform as RectTransform).anchoredPosition = Vector2.zero;
        }

        /// <summary>
        /// Calls OnPointerUp for all active pointers
        /// </summary>
        public void CancelAllPointers()
        {
            for (int i = 0; i < pointers.Count; i++)
            {
                Pointer pointer = pointers[i];

                HandlePointerUp(pointer.id, pointer.position);
            }
        }

        public void OnPointerDown(BaseEventData baseData)
        {
            if (!enabled || isAnimating || DisableTouch)
            {
                return;
            }

            PointerEventData data = baseData as PointerEventData;

            switch (state)
            {
                case State.None:
                    if (PowerUpMode)
                    {
                        // If we are in power up mode then as soon as a down event happens send a click event
                        if (OnClick != null)
                        {
                            OnClick(data.position);
                        }

                        PowerUpMode = false;
                    }
                    else
                    {
                        SetState(State.Click);
                        pointers.Add(CreatePointer(data));
                        timeTillDrag = delayTillDrag;
                    }
                    break;
                case State.Click:
                case State.Move:
                    SetState(State.Pinch);
                    pointers.Add(CreatePointer(data));
                    lastPinchDist = GetDistance(pointers[0], pointers[1]);
                    break;
            }
        }

        public void OnPointerUp(BaseEventData baseData)
        {
            if (!enabled || isAnimating || DisableTouch)
            {
                return;
            }

            PointerEventData data = baseData as PointerEventData;

            HandlePointerUp(data.pointerId, data.position);
        }

        public void OnBeginDrag(BaseEventData baseData)
        {
            if (!enabled || isAnimating || DisableTouch)
            {
                return;
            }

            PointerEventData data = baseData as PointerEventData;

            if (HasPointer(data.pointerId))
            {
                switch (state)
                {
                    case State.Click:
                        SetState(State.Move);
                        moveOffset = GetMoveOffset(pointers[0]);
                        break;
                    case State.Pinch:
                        lastPinchDist = GetDistance(pointers[0], pointers[1]);
                        break;
                }
            }
        }

        public void OnDrag(BaseEventData baseData)
        {
            if (!enabled || isAnimating || DisableTouch)
            {
                return;
            }

            PointerEventData data = baseData as PointerEventData;

            if (UpdatePointerPosition(data))
            {
                switch (state)
                {
                    case State.Drag:
                        hasFirstDragHappened = true;

                        if (OnDragged != null)
                        {
                            OnDragged(data.position);
                        }
                        break;
                    case State.Move:
                        UpdateMove();

                        if (OnMove != null)
                        {
                            OnMove(content.transform.localPosition);
                        }
                        break;
                    case State.Pinch:
                        UpdatePinch();

                        if (OnZoom != null)
                        {
                            OnZoom(content.transform.localScale);
                        }

                        break;
                }
            }
        }

        public void ZoomTo(float x, float y, float scale)
        {
            RectTransform contentRect = Content;

            isAnimating = true;
            fromValues = new Vector3(contentRect.anchoredPosition.x, contentRect.anchoredPosition.y, contentRect.localScale.x);
            toValues = new Vector3(x, y, scale);
            animTimer = 0;
            animDuration = 0.5f;
        }

        #endregion

        #region Private Methods

        private void UpdateMove()
        {
            content.transform.position = pointers[0].position - moveOffset;

            KeepInScrollArea();
        }

        private void UpdatePinch()
        {
            // Get the new scale
            float pinchDist = GetDistance(pointers[0], pointers[1]);
            float pinchDiff = pinchDist - lastPinchDist;
            float scale = Mathf.Clamp(content.transform.localScale.x + pinchDiff * zoomScale, minZoom, maxZoom);

            // Calculate the amount we need to move the picture by so we are zooming in on the pointer location
            RectTransform contentRectT = content.transform as RectTransform;
            Vector2 middle = pointers[0].position + (pointers[1].position - pointers[0].position);
            float changeInScale = scale - content.transform.localScale.x;

            Vector2 localMiddlePoint;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(contentRectT, middle, null, out localMiddlePoint);

            Vector2 offset = -changeInScale * localMiddlePoint;

            // Apply the new scale and adjust the position
            content.transform.localScale = new Vector3(scale, scale, 1f);
            contentRectT.anchoredPosition = contentRectT.anchoredPosition + offset;

            KeepInScrollArea();

            lastPinchDist = pinchDist;
        }

        private void UpdateMouseWheel()
        {
            float change = Input.mouseScrollDelta.y;

            if (change != 0)
            {
                // Get the new scale
                float scale = Mathf.Clamp(content.transform.localScale.x + change * mouseWheelZoomScale, minZoom, maxZoom);

                // Calculate the amount we need to move the picture by so we are zooming in on the pointer location
                RectTransform contentRectT = content.transform as RectTransform;
                Vector2 middle = Input.mousePosition;
                float changeInScale = scale - content.transform.localScale.x;

                Vector2 localMiddlePoint;
                RectTransformUtility.ScreenPointToLocalPointInRectangle(contentRectT, middle, null, out localMiddlePoint);

                Vector2 offset = -changeInScale * localMiddlePoint;

                // Apply the new scale and adjust the position
                content.transform.localScale = new Vector3(scale, scale, 1f);
                contentRectT.anchoredPosition = contentRectT.anchoredPosition + offset;

                KeepInScrollArea();

                if (OnZoom != null)
                {
                    OnZoom(content.transform.localScale);
                }
            }
        }

        private void HandlePointerUp(int pointerId, Vector2 position)
        {

            if (RemovePointer(pointerId))
            {
                switch (state)
                {
                    case State.Click:
                        SetState(State.None);

                        if (OnClick != null)
                        {
                            OnClick(position);
                        }
                        break;
                    case State.Drag:
                        SetState(State.None);

                        if (OnDragEnd != null)
                        {
                            OnDragEnd();
                        }
                        break;
                    case State.Move:
                        SetState(State.None);
                        break;
                    case State.Pinch:
                        SetState(State.Move);
                        ResertStartingValues();
                        break;
                }
            }
        }

        private void KeepInScrollArea()
        {
            RectTransform rectT = (content.transform as RectTransform);
            float width = rectT.rect.width * content.transform.localScale.x;
            float height = rectT.rect.height * content.transform.localScale.y;
            Vector2 pos = rectT.anchoredPosition;

            RectTransform parentRectT = transform as RectTransform;
            float parentWidth = parentRectT.rect.width;
            float parentHeight = parentRectT.rect.height;

            Vector2 topLeftCorner = new Vector2(pos.x - width / 2f, pos.y + height / 2f);
            Vector2 bottomRightCorner = new Vector2(pos.x + width / 2f, pos.y - height / 2f);

            if (topLeftCorner.x > -parentWidth / 2f)
            {
                float diff = topLeftCorner.x + parentWidth / 2f;

                rectT.anchoredPosition = new Vector2(rectT.anchoredPosition.x - diff, rectT.anchoredPosition.y);

                ResertStartingValues();
            }

            if (topLeftCorner.y < parentHeight / 2f)
            {
                float diff = topLeftCorner.y - parentHeight / 2f;

                rectT.anchoredPosition = new Vector2(rectT.anchoredPosition.x, rectT.anchoredPosition.y - diff);

                ResertStartingValues();
            }

            if (bottomRightCorner.x < parentWidth / 2f)
            {
                float diff = parentWidth / 2f - bottomRightCorner.x;

                rectT.anchoredPosition = new Vector2(rectT.anchoredPosition.x + diff, rectT.anchoredPosition.y);

                ResertStartingValues();
            }

            if (bottomRightCorner.y > -parentHeight / 2f)
            {
                float diff = bottomRightCorner.y + parentHeight / 2f;

                rectT.anchoredPosition = new Vector2(rectT.anchoredPosition.x, rectT.anchoredPosition.y - diff);

                ResertStartingValues();
            }
        }

        private void ResertStartingValues()
        {
            switch (state)
            {
                case State.Move:
                    moveOffset = GetMoveOffset(pointers[0]);
                    break;
                case State.Pinch:
                    lastPinchDist = GetDistance(pointers[0], pointers[1]);
                    break;
            }
        }

        /// <summary>
        /// Creates a new Pointer from the event data
        /// </summary>
        private Pointer CreatePointer(PointerEventData data)
        {
            Pointer pointer = new Pointer();

            pointer.id = data.pointerId;
            pointer.position = data.position;

            return pointer;
        }

        /// <summary>
        /// Removes the pointer with the given id, retuns true if a pointer was found and removed, false if there was no pointer with the given id
        /// </summary>
        private bool RemovePointer(int id)
        {
            for (int i = 0; i < pointers.Count; i++)
            {
                Pointer pointer = pointers[i];

                if (id == pointer.id)
                {
                    pointers.RemoveAt(i);

                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Determines whether this instance has pointer the specified id.
        /// </summary>
        private bool HasPointer(int id)
        {
            for (int i = 0; i < pointers.Count; i++)
            {
                Pointer pointer = pointers[i];

                if (id == pointer.id)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Updates the position of the Pointer with the given id
        /// </summary>
        private bool UpdatePointerPosition(PointerEventData data)
        {
            for (int i = 0; i < pointers.Count; i++)
            {
                Pointer pointer = pointers[i];

                if (data.pointerId == pointer.id)
                {
                    pointer.position = data.position;

                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Gets the distance between the two given pointers
        /// </summary>
        private float GetDistance(Pointer pointer1, Pointer pointer2)
        {
            return Vector2.Distance(pointer1.position, pointer2.position);
        }

        /// <summary>
        /// Gets the offset for dragging which is the vector between the given pointer and this transforms position
        /// </summary>
        private Vector2 GetMoveOffset(Pointer pointer)
        {
            return pointer.position - (Vector2)content.transform.position;
        }

        /// <summary>
        /// Calls the action.
        /// </summary>
        private void CallAction(System.Action callback)
        {
            if (callback != null)
            {
                callback();
            }
        }

        private void SetState(State newState)
        {
            state = newState;
        }

        #endregion
    }
}