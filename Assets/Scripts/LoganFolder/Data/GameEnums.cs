using UnityEngine;

public class GameEnums : MonoBehaviour
{
    public enum ElementType{None, Fire, Ice, Stone}
    public enum StatusEffect{None, Burning, Chilled, Concussed}
    public enum StatType{Damage, AttackSpeed, Knockback, CritChance, StaggerDamage, WallSlamDamage, ParryStaggerAmount, CurrentHealth, MaxHealth, HealthRegenRate, LifeSteal}
}
