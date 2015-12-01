using UnityEngine;
using System.Collections;
using UnityEngine.Networking;


/// <summary>
/// ProtoMech
/// Defines the proto characteristics of our mechs. 
/// </summary>
public class ProtoMech : MonoBehaviour
{
   public int ProtoID;
   public string DisplayName;
   public float MaxHealth;
   public float MaxSpeed;
   public float MaxTurn;
   public float MaxTurrentAngle;
   public float MaxHeat;
   public float CoolingRate;     // Units of Heat/Second the mech cools for
	
}
