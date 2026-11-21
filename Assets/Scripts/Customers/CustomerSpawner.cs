using System.Collections.Generic;
using UnityEngine;

/// <summary>Object-pooled customer spawner. Max three customers at once; day rules come from DayConfig.</summary>
public class CustomerSpawner : MonoBehaviour
{
    public List<Customer> Pool = new List<Customer>();   // three pre-placed customers, one per queue spot
    public Transform[] Spots;
    DayConfig day;
    float timer;
    bool running;

    public void BeginDay(DayConfig config)
    {
        day = config; timer = 0.3f; running = true;
        foreach (var c in Pool) c.gameObject.SetActive(false);
    }

    public void Stop()
    {
        running = false;
        foreach (var c in Pool) c.gameObject.SetActive(false);
    }

    int ActiveCount() { int n = 0; foreach (var c in Pool) if (c.gameObject.activeSelf) n++; return n; }

    void Update()
    {
        if (!running) return;
        timer -= Time.deltaTime;
        if (timer > 0f || ActiveCount() >= day.MaxWaiting) return;

        for (int i = 0; i < Pool.Count && i < day.MaxWaiting; i++)
        {
            if (Pool[i].gameObject.activeSelf) continue;
            if (Spots != null && i < Spots.Length) Pool[i].transform.position = Spots[i].position;
            Pool[i].Arrive(RandomOrder(), day.PatienceSeconds * GameSettings.PatienceMult);
            timer = day.SpawnGap;
            break;
        }
    }

    List<ItemType> RandomOrder()
    {
        int n = Random.Range(day.MinItems, day.MaxItems + 1);
        var list = new List<ItemType>();
        float drinkBias = Mathf.Max(day.DrinkBias, HeatManager.Instance != null ? HeatManager.Instance.DrinkBias : 0f);
        for (int i = 0; i < n; i++)
        {
            bool drink = Random.value < Mathf.Lerp(0.33f, 0.8f, drinkBias);
            list.Add(drink ? (Random.value < 0.5f ? ItemType.Water : ItemType.Soda)
                           : (ItemType)Random.Range(0, 4));
        }
        return list;
    }
}
