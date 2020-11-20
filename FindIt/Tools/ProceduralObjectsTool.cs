using System;
using System.Collections.Generic;
using UnityEngine;
using System.Collections;

namespace FindIt
{
    public class ProceduralObjectsTool
    {
        private Dictionary<string, uint> poInstanceCount = new Dictionary<string, uint>();

        public void UpdatePOInfoList()
        {
            poInstanceCount.Clear();

            try
            {
                GameObject gameLogicObject = GameObject.Find("Logic_ProceduralObjects");

                Type ProceduralObjectsLogicType = Type.GetType("ProceduralObjects.ProceduralObjectsLogic");

                Type ProceduralObjectType = Type.GetType("ProceduralObjects.Classes.ProceduralObject");

                Component logic = gameLogicObject.GetComponent("ProceduralObjectsLogic");

                object poList = ProceduralObjectsLogicType.GetField("proceduralObjects").GetValue(logic);

                foreach (var i in poList as IList)
                {
                    string basePrefabName = ProceduralObjectType.GetField("basePrefabName").GetValue(i).ToString();
                    string infoType = ProceduralObjectType.GetField("baseInfoType").GetValue(i).ToString();
                    if (basePrefabName != null && infoType != null)
                    {
                        // Debugging.Message("PO instance found - " + basePrefabName.ToString());

                        if (!poInstanceCount.ContainsKey(basePrefabName))
                        {
                            poInstanceCount.Add(basePrefabName, 1);
                        }
                        else
                        {
                            poInstanceCount[basePrefabName] += 1;
                        }
                    }
                }
            }
            catch(Exception e)
            {
                Debugging.LogException(e);
            }
        }

        public uint GetPrefabPOInstanceCount(PrefabInfo info)
        {
            if (!(info is PropInfo) && !(info is BuildingInfo)) return 0;

            if (poInstanceCount.ContainsKey(info.name)) return poInstanceCount[info.name];
            
            return 0;
        }
    }
}
