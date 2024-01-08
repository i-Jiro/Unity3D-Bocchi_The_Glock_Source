using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(-1)]
public class PlayerInputManager : MonoBehaviour
{
    private Vector3 _mousePosition = Vector3.zero;
    private Camera _mainCamera;
    
    public static PlayerInputManager Instance;
    public Vector3 MousePosition => _mousePosition;

    public bool Mouse1Down = false;

    public delegate void AimKeyPressEventHandler();
    public event AimKeyPressEventHandler AimKeyPressed;

    public delegate void AimKeyReleaseEventHandler();
    public event AimKeyPressEventHandler AimKeyReleased;

    private void Awake()
    {
        if (Instance)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        _mainCamera = Camera.main;
    }

    // Update is called once per frame
    void Update()
    {
        _mousePosition = Input.mousePosition;
        Mouse1Down = Input.GetKey(KeyCode.Mouse1);
        if (Input.GetKeyDown(KeyCode.Mouse1))
        {
            AimKeyPressed?.Invoke();
        }
        else if(Input.GetKeyUp(KeyCode.Mouse1))
        {
            AimKeyReleased?.Invoke();
        }
    }
}
