using System;
using UnityEngine;

/// <summary>Walks a person along a list of waypoints (no NavMesh needed), then turns to face a direction.</summary>
[RequireComponent(typeof(PersonAnim))]
public class PersonMover : MonoBehaviour
{
    public float WalkSpeed = 1.25f;
    public float TurnSpeed = 300f;
    PersonAnim anim;
    Vector3[] path; int index; bool moving; Vector3 finalFacing; Action done;

    void Awake() { anim = GetComponent<PersonAnim>(); }

    public bool IsMoving => moving;

    public void MoveAlong(Vector3[] points, Vector3 facingAtEnd, Action onArrive)
    {
        path = points; index = 0; moving = points != null && points.Length > 0;
        finalFacing = facingAtEnd; done = onArrive;
        if (!moving) onArrive?.Invoke();
    }

    public void Stop() { moving = false; done = null; if (anim != null) anim.SetSpeed(0f); }

    void Update()
    {
        if (!moving)
        {
            if (anim != null) anim.SetSpeed(0f);
            if (finalFacing.sqrMagnitude > 0.01f) Face(finalFacing);
            return;
        }
        Vector3 target = path[index];
        Vector3 to = target - transform.position; to.y = 0f;
        float step = WalkSpeed * Time.deltaTime;
        if (to.magnitude <= step)
        {
            transform.position = new Vector3(target.x, transform.position.y, target.z);
            index++;
            if (index >= path.Length) { moving = false; var cb = done; done = null; cb?.Invoke(); }
        }
        else
        {
            Face(to);
            transform.position += to.normalized * step;
        }
        if (anim != null) anim.SetSpeed(1f);
    }

    void Face(Vector3 dir)
    {
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.0001f) return;
        var q = Quaternion.LookRotation(dir.normalized, Vector3.up);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, q, TurnSpeed * Time.deltaTime);
    }
}
