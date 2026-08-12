using UnityEngine;

public abstract class EnemyCatchBehavior : MonoBehaviour
{
    //used to interrupt enemy attacks from hitting the player
    public virtual bool BlocksDamage => false;

    //method to trigger the grab functions
    public abstract void OnGrab(EnemyAI self);

    //method to call every action when throwing an enemy
    public abstract void OnThrow(EnemyAI self);
}