using UnityEngine;
using System.Collections;

public class Utility
{
   /// <summary>
   /// Simple function to walk the parent chain of a passed in object,
   /// looking for the first object that has the asked for Component.
   /// </summary>
   /// <typeparam name="T"></typeparam>
   /// <param name="thisObj"></param>
   /// <returns></returns>
   public static T GetComponentInParent<T>(GameObject thisObj)
   {
      // First, check this object
      T returnComp = default(T);
      returnComp = thisObj.GetComponent<T>();
      if (returnComp != null)
         return returnComp;
      Transform currTransform = thisObj.transform;
      while (currTransform.parent != null)
      {
         GameObject parentObj = currTransform.parent.gameObject;
         returnComp = parentObj.GetComponent<T>();
         if (returnComp != null)
            return returnComp;
         currTransform = currTransform.parent;
      }

      return returnComp;
      
   }


}
