﻿//https://stackoverflow.com/questions/58328209/how-to-make-a-free-fly-camera-script-in-unity-with-acceleration-and-decceleratio
// copyright Runette Software Ltd, 2020. All rights reserved
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;


public class FlyingCam : MonoBehaviour
{
    [Header("Constants")]

    //unity controls and constants input
    public float AccelerationMod;
    public float DeccelerationMod;
    public float XAxisSensitivity;
    public float YAxisSensitivity;

    [Space]

    [Range(0, 89)] public float MaxXAngle = 60f;

    [Space]

    public float MaximumMovementSpeed = 1f;


    private bool editSelected = false;
    public Camera self;
    private Rigidbody selectedRigibody;
    private float selectedDistance;
    private Vector3 speed;

    public EventManager eventManager;


    private void Start()
    {
    }

    // Update is called once per frame
    private void Update()
    {
        transform.Translate(speed);
        OVRInput.Update();
        speed -= speed / DeccelerationMod;
    }

    private void FixedUpdate()
    {
        OVRInput.FixedUpdate();
    }

    public void HandleMove(InputAction.CallbackContext context)
    {
        Vector3 speed_input = context.ReadValue<Vector2>().normalized * AccelerationMod;
        speed += Quaternion.AngleAxis(90.0f, Vector3.right) * speed_input;
    }

    public void HandleVertical(InputAction.CallbackContext context)
    {
        Vector3 speed_input = context.ReadValue<Vector2>().normalized * AccelerationMod;
        speed += speed_input;
    }

    public void HandlePanZoom(InputAction.CallbackContext context)
    {
        Vector2 pz_input = context.ReadValue<Vector2>().normalized;
        float pan = pz_input.x;
        float zoom = pz_input.y;
            transform.LookAt(Vector3.zero);
            gameObject.transform.RotateAround(Vector3.zero, Vector3.up, pan);
            transform.Translate(Vector3.forward * Vector3.Distance(transform.position, Vector3.zero) * 0.1f * zoom);
    }

    private float _rotationX;

    public void HandleMouseRotation(InputAction.CallbackContext context)
    {
        Vector2 input = context.ReadValue<Vector2>();
        if (!editSelected)
        {
            //mouse input
            var rotationHorizontal = XAxisSensitivity * input.x;
            var rotationVertical = YAxisSensitivity * input.y;

            //applying mouse rotation
            // always rotate Y in global world space to avoid gimbal lock
            transform.Rotate(Vector3.up * rotationHorizontal, Space.World);

            var rotationY = transform.localEulerAngles.y;

            _rotationX += rotationVertical;
            _rotationX = Mathf.Clamp(_rotationX, -MaxXAngle, MaxXAngle);

            transform.localEulerAngles = new Vector3(-_rotationX, rotationY, 0);
        }
        else
        {
            Ray ray = self.ScreenPointToRay(Mouse.current.position.ReadValue());
            Vector3 newPos = ray.GetPoint(selectedDistance);
            if (selectedRigibody != null)
            {
                selectedRigibody.gameObject.SendMessage("MoveTo", newPos);
            }
        }
    }

    public void HandleKeyInput(InputAction.CallbackContext context)
    {
        InputAction action = context.action;
        if (action.name == "Edit" && !Global.EditSession)
        {
            Global.EditSession = true;
        }
        if (action.name == "EndEdit" && Global.EditSession)
        {
            Global.EditSession = false;
            EventManager eventManager = Global.Map.GetComponent<EventManager>();
            eventManager.OnEditsessionEnd.Invoke();
        }
        if (action.name == "Exit")
        {
            Debug.Log("Exit");
            Application.Quit();
        }
    }

    public void HandleMouseClick(InputAction.CallbackContext context)
    {
        InputAction action = context.action;
        int button = 0;
        switch (action.name)
        {
            case "Select":
                button = 0;
                break;
            case "MultiSelect":
                button = 1;
                break;
        }
        if (action.phase == InputActionPhase.Canceled && Global.EditSession)
        {
            UnClickHandler(button);
        }
        else if (action.phase == InputActionPhase.Started && !editSelected && Global.EditSession)
        {
            ClickHandler(button);
        }
    }

    private void ClickHandler(int button)
    {
        RaycastHit hitInfo = new RaycastHit();
        bool hit = Physics.Raycast(self.ScreenPointToRay(Mouse.current.position.ReadValue()), out hitInfo);
        if (hit)
        {
            selectedRigibody = hitInfo.rigidbody;
            if (selectedRigibody != null)
            {
                editSelected = true;
                selectedRigibody.gameObject.SendMessage("Selected", button);
                selectedDistance = hitInfo.distance;
            }
        }
        else
        {
            editSelected = false;
        }
    }


    private void UnClickHandler(int button)
    {
        editSelected = false;
        if (selectedRigibody != null)
        {
            selectedRigibody.gameObject.SendMessage("UnSelected", button);
            selectedRigibody = null;
        }
    }

}
