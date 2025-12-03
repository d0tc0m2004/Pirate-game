using UnityEngine;

[CreateAssetMenu(fileName = "NewHazard", menuName = "Tactical/Hazard Data")]
public class HazardData : ScriptableObject
{
    [Header("Visuals")]
    public string hazardName;
    public GameObject hazardPrefab;

    [Header("Grid Rules")]
    public bool isBlocking;         
    public bool causesDisplacement; 
    public HazardShape shapePattern;

    [Header("Destruction")]
    public bool isDestructible;     
    public int maxHealth;           
    public GameObject dropItem;     

    [Header("Effect Logic")]
    public HazardEffectType effectType; 

    [Header("Stats")]
    public int damageHP;            
    public int damageMorale;        
    public float curseMultiplier;   
    
    
    public int effectDuration;      
    

    public enum HazardEffectType 
    { 
        None,          
        Box,            
        Fire,           
        Plague,         
        ShiftingSand,   
        Lightning,      
        Trap,           
        Cursed          
    }

    public enum HazardShape { Single, Row, Column, Square, Plus }
}