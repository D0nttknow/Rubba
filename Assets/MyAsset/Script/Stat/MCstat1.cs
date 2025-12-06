using System;
using UnityEngine;

/// <summary>
/// Main character stat container that implements ICharacterStat and IHpProvider,
/// subscribes to PlayerLevel (if present) and raises events for UI.
/// Reworked to correctly implement the interface members required by the project.
/// </summary>
public class MCstat1 : MonoBehaviour, ICharacterStat, IHpProvider
{
    [Header("Player Base Stats")]
    public string playerName = "Main Knight";
    public int hp = 20;
    public int maxHp = 20;
    public int atk = 15;
    public int def = 6;
    public int speed = 10;

    [Header("Local level (kept in sync with PlayerLevel when available)")]
    [Tooltip("This value will be overwritten when PlayerLevel provider is present.")]
    public int level = 1;

    [Header("Level provider (optional)")]
    public PlayerLevel playerLevel; // optional reference (drag in inspector)

    // events for UI
    public event Action<int, int> OnHpChanged; // (current, max)
    public event Action<int> OnLevelChanged;   // (new level)

    // IHpProvider
    public int CurrentHp => hp;
    public int MaxHp => maxHp;

    // ICharacterStat implementation (names must match the interface)
    public string Name => playerName;
    public int hpStat => hp; // keep legacy accessor if other code uses hpStat; not required by interface
    public int maxHpStat => maxHp;
    public int atkStat => atk;
    public int defStat => def;
    public int speedStat => speed;
    public int levelStat => level;

    // Interface-required properties (exact names expected by ICharacterStat)
    // If your ICharacterStat expects properties named hp, maxHp, atk, def, speed, level,
    // we expose those via explicit properties below:
    int ICharacterStat.hp { get => hp; }
    int ICharacterStat.maxHp { get => maxHp; }
    int ICharacterStat.atk { get => atk; }
    int ICharacterStat.def { get => def; }
    int ICharacterStat.speed { get => speed; }
    int ICharacterStat.level { get => level; }
    string ICharacterStat.Name { get => playerName; }

    void Start()
    {
        // clamp hp
        if (maxHp <= 0) maxHp = Mathf.Max(1, hp);
        hp = Mathf.Clamp(hp, 0, maxHp);

        // try subscribe to PlayerLevel
        EnsurePlayerLevelSubscription();

        // initial UI notification
        OnHpChanged?.Invoke(hp, maxHp);
        OnLevelChanged?.Invoke(level);
    }

    void EnsurePlayerLevelSubscription()
    {
        if (playerLevel == null)
        {
            playerLevel = GetComponent<PlayerLevel>() ?? FindObjectOfType<PlayerLevel>();
        }

        if (playerLevel != null)
        {
            // sync immediately
            SyncLevelFromProvider(playerLevel.Level);

            // subscribe
            playerLevel.OnLevelChanged += OnProviderLevelChanged;
        }
        else
        {
            Debug.LogWarning($"{name}: PlayerLevel provider not found. MCstat1 will use its local level value.");
        }
    }

    void OnDestroy()
    {
        if (playerLevel != null)
            playerLevel.OnLevelChanged -= OnProviderLevelChanged;
    }

    void OnProviderLevelChanged(int newLevel)
    {
        SyncLevelFromProvider(newLevel);
    }

    void SyncLevelFromProvider(int providerLevel)
    {
        level = Mathf.Max(1, providerLevel);
        // optionally recalc stats here if you have level-based scaling
        OnLevelChanged?.Invoke(level);
        Debug.Log($"[MCstat1] Synced level to {level}");
    }

    // public API: take damage / heal
    public void TakeDamage(int damage)
    {
        Debug.Log($"TakeDamage on {gameObject.name} dmg={damage}");
        int applied = Mathf.Max(1, damage - def);
        hp -= applied;
        hp = Mathf.Clamp(hp, 0, maxHp);
        OnHpChanged?.Invoke(hp, maxHp);
        if (hp <= 0) Die();
    }

    public void Heal(int amount)
    {
        if (amount <= 0) return;
        hp = Mathf.Clamp(hp + amount, 0, maxHp);
        OnHpChanged?.Invoke(hp, maxHp);
    }

    void Die()
    {
        Debug.Log($"{Name} died");
        // cleanup or respawn logic
    }

    // Implement interface attack methods required by ICharacterStat
    // These call into the typical monster interface
    public void AttackMonster(IMonsterStat monster)
    {
        if (monster == null) return;
        monster.TakeDamage(atk);
        Debug.Log($"{Name} attacked {monster} for {atk}");
    }

    public void StrongAttackMonster(IMonsterStat monster)
    {
        if (monster == null) return;
        int strongAtk = atk * 2;
        monster.TakeDamage(strongAtk);
        Debug.Log($"{Name} strong-attacked {monster} for {strongAtk}");
    }
}