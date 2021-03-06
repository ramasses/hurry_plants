﻿using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

public class Player : SimpleStateMachine
{
    [SerializeField] private int playerIndex;
    
    [SerializeField] private Pickable pickable;
    [SerializeField] private Picker picker;
    [SerializeField] private AirMovement airMovement;
    [SerializeField] private GroundMovement groundMovement;
    [SerializeField] private Respawner respawner;
    [SerializeField] private GameObject pickMeFx;

    private InputHandler inputHandler;
    
    private enum PlayerStates { Idle, Running, Flying, Captured }
    
    [HideInInspector] public UnityEvent OnPlayerDie = new UnityEvent();

    private void Awake()
    {
    }

    private void Start()
    {
        PlayerInputManager.instance.onPlayerJoined += OnPlayerJoined;
        
        pickable.OnPicked.AddListener(() => currentState = PlayerStates.Captured);
        pickable.OnThrowed.AddListener(() => currentState = PlayerStates.Flying);
        
        pickable.OnHit.AddListener(OnHit);
        
        currentState = PlayerStates.Idle;
    }

    private void OnDestroy()
    {
        PlayerInputManager.instance.onPlayerJoined -= OnPlayerJoined;
    }

    private void OnPlayerJoined(PlayerInput playerInput)
    {
        if (playerInput.playerIndex == playerIndex)
            inputHandler = playerInput.GetComponent<InputHandler>();
    }

    private void OnPick()
    {
        pickable.IsPickBlocked = true;
    }

    private void OnHit(Pickable pickable, GameObject other) => Land();

    private void Land()
    {
        currentState = PlayerStates.Idle;
        transform.position = new Vector3(transform.position.x, 0, transform.position.z);
    }
    
    public void OnColliderEnter(Type type)
    {
        Debug.LogWarning("OnColliderEnter : " + type.Name);
    }

    protected override void EarlyGlobalSuperUpdate()
    {
        if (!inputHandler) return;
        
        if (inputHandler.ThrowButton)
            picker.Throw();

        if (Input.GetKeyDown(KeyCode.R))
        {
            var bombs = FindObjectsOfType<Pickable>();
            foreach (var b in bombs)
                Physics.IgnoreCollision(GetComponent<Collider>(), b.Collider, false);
        }
    }

    protected override void LateGlobalSuperUpdate()
    {
        if (!pickable.IsPickBlocked)
        {
            Debug.LogWarning(gameObject.name + " PickBlocked =" + pickable.IsPickBlocked);
            if (!pickMeFx.activeInHierarchy)
                pickMeFx.SetActive(true);
        }
        else
        {
            if (pickMeFx.activeInHierarchy)
                pickMeFx.SetActive(false);
        }
    }

    private void Running_EnterState()
    {
        groundMovement.SetTrail(true);
    }
    
    private void Running_Update()
    {
        if (!picker.IsBusy)
            pickable.IsPickBlocked = !inputHandler.PickMeButton;
    }
    
    private void Running_FixedUpdate()
    {
        if (inputHandler.Direction != Vector3.zero) //Comment this to enable always moving mechanic
        {
            if (groundMovement.InsideWaterStream)
                groundMovement.Move(inputHandler.Direction, 0.35f);
            else
                groundMovement.Move(inputHandler.Direction);
        }
        else
        {
            currentState = PlayerStates.Idle;
        }
    }

    private void Running_ExitState()
    {
        groundMovement.SetTrail(false);
    }

    private void Idle_EnterState()
    {
        pickable.SetIdle();
//        pickable.IsPickBlocked = false;
        picker.Unavaiable = false;
    }

    private void Idle_Update()
    {
        if (!inputHandler) return;
        
        if (!picker.IsBusy)
            pickable.IsPickBlocked = !inputHandler.PickMeButton;
        
        if (inputHandler.Direction != Vector3.zero)
            currentState = PlayerStates.Running;
    }

    private void Idle_ExitState()
    {
//        pickable.IsPickBlocked = true;
    }

    private void Captured_EnterState()
    {
        groundMovement.LeaveWaterStream();
        picker.Unavaiable = true;
        pickable.IsPickBlocked = true;
    }

    private void Captured_Update()
    {
        if (inputHandler.ThrowButton)
        {
            pickable.GetRelease();
            currentState = PlayerStates.Idle;
        }
    }

    private void Captured_ExitState()
    {
    }

    private void Flying_Update()
    {
        if (inputHandler.ThrowButton) 
            Land();
    }

    public void KillBy(KillType killType)
    {
        if (respawner.IsRespawning) return;
        
        picker.OnPickerDie.Invoke();
        respawner.Register();
        
        Debug.Log($"{gameObject.name} was killed by {killType}");

        switch (killType)
        {
            case KillType.Cactus:
                break;
            case KillType.Hole:
                break;
            case KillType.Bomb:
                break;
            case KillType.Victim:
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(killType), killType, null);
        }
    }
    
    private void OnDrawGizmos()
    {
        var from = transform.position + Vector3.up;
        var to = from + transform.forward * 1.5f;
        Gizmos.DrawLine(from, to);
    }
}
