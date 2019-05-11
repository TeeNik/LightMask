using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public readonly float MoveSpeed = 6;

    private Rigidbody _rb;
    private Camera _viewCamera;
    private Vector3 _velocity;

    void Start()
    {
        _rb = GetComponent<Rigidbody>();
        _viewCamera = Camera.main;
    }

    void Update()
    {
        Vector3 mousePos = _viewCamera.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, _viewCamera.transform.position.y));
        transform.LookAt(mousePos + Vector3.up * transform.position.y);
        _velocity = new Vector3(Input.GetAxisRaw("Horizontal"), 0, Input.GetAxisRaw("Vertical")).normalized * MoveSpeed;
    }

    private void FixedUpdate()
    {
        _rb.MovePosition(_rb.position + _velocity * Time.fixedDeltaTime);
    }
}
