using UnityEngine;

/// <summary>Background market-goer who strolls back and forth along a street lane. Pure ambience.</summary>
public class Pedestrian : MonoBehaviour
{
    public Vector3 A, B;
    public float Speed = 1.1f;
    public float PauseSeconds = 2.5f;
    PersonAnim anim; Vector3 target; float pause;

    void Start()
    {
        anim = GetComponent<PersonAnim>();
        target = B;
        if (anim != null) anim.Randomise(Random.value);
    }

    void Update()
    {
        if (pause > 0f)
        {
            pause -= Time.deltaTime; if (anim != null) anim.SetSpeed(0f);
            if (pause <= 0f && Random.value < 0.4f && anim != null) anim.Gesture("Talk");
            return;
        }
        Vector3 to = target - transform.position; to.y = 0f;
        float step = Speed * Time.deltaTime;
        if (to.magnitude <= step)
        {
            target = target == B ? A : B;
            pause = Random.Range(0f, PauseSeconds);
        }
        else
        {
            transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(to.normalized), 240f * Time.deltaTime);
            transform.position += to.normalized * step;
            if (anim != null) anim.SetSpeed(1f);
        }
    }
}
