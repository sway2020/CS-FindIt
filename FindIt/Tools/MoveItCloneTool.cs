// ugly hack to use Move It to place props
// basically it fakes a Move It export file and calls Move It to import the file

using System;
using System.Collections.Generic;
using UnityEngine;
using System.Collections;
using System.Reflection;
using System.IO;
using ColossalFramework.IO;

namespace FindIt
{
    public static class MoveItCloneTool
    {
        private static bool initialized = false;
        private static ToolController toolController;
        private static Component moveItTool;
        private static MethodInfo importMI;
        private static void Init()
        {
            try
            {
                toolController = UnityEngine.Object.FindObjectOfType<ToolController>();
                moveItTool = toolController.GetComponent("MoveItTool");
                Type MoveItToolType = Type.GetType("MoveIt.MoveItTool");
                importMI = MoveItToolType.GetMethod("Import", new Type[] { typeof(string) });
            }
            catch(Exception ex)
            {
                Debugging.LogException(ex);
            }

            initialized = true;
        }

        public static void MoveItClone(PrefabInfo prefab)
        {
            if (!initialized) Init();

            // make a fake move it export xml
            FakeMoveItExport(prefab);
            
            // call move it Import
            toolController.CurrentTool = moveItTool as ToolBase;
            importMI.Invoke(moveItTool, new object[] { "FindIt2FakeMoveItExport" });

            // delete fake move it export xml
            DeleteFakeMoveItExport();
        }

        private static void FakeMoveItExport(PrefabInfo prefab)
        {
            string path = Path.Combine(DataLocation.localApplicationData, "MoveItExports");
            if (!File.Exists(path))
            {
                System.IO.Directory.CreateDirectory(path);
            }

            path = Path.Combine(path, "FindIt2FakeMoveItExport.xml");
            if (File.Exists(path)) File.Delete(path);

            if (prefab is PropInfo)
            {
                FakePropExport(path, prefab as PropInfo);
            }
        }
        public static void DeleteFakeMoveItExport()
        {
            string path = Path.Combine(DataLocation.localApplicationData, "MoveItExports");
            path = Path.Combine(path, "FindIt2FakeMoveItExport.xml");
            if (File.Exists(path)) File.Delete(path);
        }

        private static void FakePropExport(string path, PropInfo propInfo)
        {
            using (System.IO.StreamWriter file = new System.IO.StreamWriter(path, true))
            {
                file.WriteLine("<?xml version=\"1.0\" encoding=\"utf-8\"?>");
                file.WriteLine("<Selection>");
                file.WriteLine("  <center>");
                file.WriteLine("    <x>413.9925</x>");
                file.WriteLine("    <y>89.59375</y>");
                file.WriteLine("    <z>-685.2718</z>");
                file.WriteLine("  </center>");
                file.WriteLine("  <state d2p1:type=\"PropState\" xmlns:d2p1=\"http://www.w3.org/2001/XMLSchema-instance\">");
                file.WriteLine("    <position>");
                file.WriteLine("      <x>413.9925</x>");
                file.WriteLine("      <y>89.59375</y>");
                file.WriteLine("      <z>-685.2718</z>");
                file.WriteLine("    </position>");
                file.WriteLine("    <angle>0</angle>");
                file.WriteLine("    <terrainHeight>89.61153</terrainHeight>");
                if (propInfo.m_isCustomContent) file.WriteLine($"    <isCustomContent>true</isCustomContent>");
                else file.WriteLine($"    <isCustomContent>false</isCustomContent>");
                file.WriteLine("    <id>167834578</id>");
                file.WriteLine($"    <prefabName>{propInfo.name}</prefabName>");
                file.WriteLine("    <IntegrationEntry_List />");
                file.WriteLine("    <single>true</single>");
                file.WriteLine("    <fixedHeight>false</fixedHeight>");
                file.WriteLine("  </state>");
                file.WriteLine("</Selection>");
            }
        }

    }
}
