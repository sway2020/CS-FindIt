using System;
using System.Collections.Generic;
using System.IO;
using ColossalFramework;
using FindIt.GUI;
using ColossalFramework.UI;
using ColossalFramework.IO;
using UnityEngine;
using System.Reflection;

namespace FindIt
{
    public static class LocateNextInstanceTool
    {
        private static uint propInstanceCounter = 0;
        private static uint treeInstanceCounter = 0;
        private static uint buildingInstanceCounter = 0;
        private static uint networkSegmentInstanceCounter = 0;

        public static CameraController cameraController;
        public static PrefabInfo selectedPrefab;
        
        public static void Initialize()
        {
            // Find camera controller
            GameObject mainCamera = GameObject.FindGameObjectWithTag("MainCamera");
            if (mainCamera != null)
            {
                cameraController = mainCamera.GetComponent<CameraController>();
            }
        }

        public static void LocateNextInstance(bool findPOInstance)
        {
            if (selectedPrefab == null) return;

            if (findPOInstance)
            {
                if (!FindIt.isPOEnabled) return;
                if (!(selectedPrefab is BuildingInfo || selectedPrefab is PropInfo)) return;
                LocateNextPOInstance(selectedPrefab);
                return;
            }

            if (selectedPrefab is PropInfo) LocateNextPropDecalInstance(selectedPrefab);
            else if (selectedPrefab is BuildingInfo) LocateNextBuildingInstance(selectedPrefab);
            else if (selectedPrefab is TreeInfo) LocateNextTreeInstance(selectedPrefab);
            else if (selectedPrefab is NetInfo) LocateNextNetworkSegmentInstance(selectedPrefab);
        }

        private static void SetCameraPosition(Vector3 targetPosition)
        {
            cameraController.m_targetAngle.y = 90f;
            cameraController.m_targetPosition = targetPosition;
        }

        private static void LocateNextPOInstance(PrefabInfo prefab)
        {
            Vector3 position = ProceduralObjectsTool.GetPOInstancePosition(prefab);
            if (position == Vector3.zero) return;
            SetCameraPosition(position);
        }

        private static void LocateNextPropDecalInstance(PrefabInfo prefab)
        {
            Array16<PropInstance> props = PropManager.instance.m_props;
            for (uint i = (propInstanceCounter + 1) % props.m_size; i != propInstanceCounter; i = (i + 1) % props.m_size)
            {
                if (props.m_buffer[i].Info == prefab)
                {
                    bool isValid = ((PropInstance.Flags)props.m_buffer[i].m_flags != PropInstance.Flags.None && (PropInstance.Flags)props.m_buffer[i].m_flags != PropInstance.Flags.Deleted) ;
                    if (!isValid) continue;
                    SetCameraPosition(props.m_buffer[i].Position);
                    propInstanceCounter = (i + 1) % props.m_size;
                    return;
                }
            }
            propInstanceCounter = 0;
        }

        private static void LocateNextTreeInstance(PrefabInfo prefab)
        {
            Array32<TreeInstance> trees = TreeManager.instance.m_trees;
            for (uint i = (treeInstanceCounter + 1) % trees.m_size; i != treeInstanceCounter; i = (i + 1) % trees.m_size)
            {
                if (trees.m_buffer[i].Info == prefab)
                {
                    bool isValid = ((TreeInstance.Flags)trees.m_buffer[i].m_flags != TreeInstance.Flags.None && (TreeInstance.Flags)trees.m_buffer[i].m_flags != TreeInstance.Flags.Deleted);
                    if (!isValid) continue;
                    SetCameraPosition(trees.m_buffer[i].Position);
                    treeInstanceCounter = (i + 1) % trees.m_size;
                    return;
                }
            }
            treeInstanceCounter = 0;
        }

        private static void LocateNextBuildingInstance(PrefabInfo prefab)
        {
            Array16<Building> buildings = BuildingManager.instance.m_buildings;
            for (uint i = (buildingInstanceCounter + 1) % buildings.m_size; i != buildingInstanceCounter; i = (i + 1) % buildings.m_size)
            {
                if (buildings.m_buffer[i].Info == prefab)
                {
                    bool isValid = (buildings.m_buffer[i].m_flags != Building.Flags.None && buildings.m_buffer[i].m_flags != Building.Flags.Deleted);
                    if (!isValid) continue;
                    SetCameraPosition(buildings.m_buffer[i].m_position);
                    buildingInstanceCounter = (i + 1) % buildings.m_size;
                    return;
                }
            }
            buildingInstanceCounter = 0;
        }

        private static void LocateNextNetworkSegmentInstance(PrefabInfo prefab)
        {
            Array16<NetSegment> segments = NetManager.instance.m_segments;
            for (uint i = (networkSegmentInstanceCounter + 1) % segments.m_size; i != networkSegmentInstanceCounter; i = (i + 1) % segments.m_size)
            {
                if (segments.m_buffer[i].Info == prefab)
                {
                    bool isValid = (segments.m_buffer[i].m_flags != NetSegment.Flags.None && segments.m_buffer[i].m_flags != NetSegment.Flags.Deleted);
                    if (!isValid) continue;
                    SetCameraPosition(segments.m_buffer[i].m_middlePosition);
                    networkSegmentInstanceCounter = (i + 1) % segments.m_size;
                    return;
                }
            }
            networkSegmentInstanceCounter = 0;
        }
    }
}
