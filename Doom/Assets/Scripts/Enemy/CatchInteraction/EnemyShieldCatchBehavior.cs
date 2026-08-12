using UnityEngine;

public class EnemyShieldCatchBehavior : EnemyCatchBehavior
{
    public override bool BlocksDamage => true;

    public override void OnGrab(EnemyAI self)
    {
        
    }

    public override void OnThrow(EnemyAI self)
    {
        
    }
}