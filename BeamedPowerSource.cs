﻿using System;
using System.IO;
using System.Text;
using UnityEngine;

namespace BeamedPowerStandalone
{
    public class WirelessSource : PartModule
    {
        // creating things on part right click menu (flight)
        [KSPField(guiName = "Power Transmitter", isPersistant = true, guiActive = true, guiActiveEditor = false), UI_Toggle(scene = UI_Scene.Flight)]
        public bool Transmitting;

        [KSPField(guiName = "Beamed Power", isPersistant = true, guiActive = true, guiActiveEditor = false, guiUnits = "EC/s", guiActiveUnfocused = true, unfocusedRange = 10000000000000)]
        public float excess;

        [KSPField(guiName = "Power to Beam", isPersistant = true, guiActive = true, guiActiveEditor = false, guiUnits = "EC/s"), UI_FloatRange(minValue = 0, maxValue = 100000, stepIncrement = 1, scene = UI_Scene.Flight)]
        public float powerBeamed;

        // variables whose values (if they have one) are written to savefile
        [KSPField(isPersistant = true)]
        public string TransmittingTo;

        [KSPField(isPersistant = true)]
        public float constant;

        // 'dish_diameter', 'efficiency', and 'wavelength' are set in part .cfg file:
        [KSPField(isPersistant = false)]
        public float DishDiameter;

        [KSPField(isPersistant = false)]
        public string Wavelength;

        [KSPField(isPersistant = false)]
        public float Efficiency;

        // getting resource id of 'Electric Charge'
        public int EChash = PartResourceLibrary.Instance.GetDefinition("ElectricCharge").id; int frames = 0;

        // setting action group capability
        [KSPAction(guiName = "Toggle Power Transmitter")]
        public void ToggleBPTransmitter(KSPActionParam param)
        {
            if (Transmitting == true)
            {
                Transmitting = false;
            }
            else
            {
                Transmitting = true;
            }
        }

        [KSPAction(guiName = "Activate Power Transmitter")]
        public void ActivateBPTransmitter(KSPActionParam param)
        {
            if (Transmitting == false)
            {
                Transmitting = true;
            }
        }

        [KSPAction(guiName = "Deactivate Power Transmitter")]
        public void DeactivateBPTransmitter(KSPActionParam param)
        {
            if (Transmitting == true)
            {
                Transmitting = false;
            }
        }

        // adding part info to part description tab in editor
        public string GetModuleTitle()
        {
            return "Wireless Source";
        }
        public override string GetModuleDisplayName()
        {
            return "Beamed Power Transmitter";
        }
        public override string GetInfo()
        {
            return ("Dish Diameter: " + Convert.ToString(DishDiameter) + "\n" 
                + "EM Wavelength: " + Convert.ToString(Wavelength) + "\n" 
                + "Efficiency: " + Convert.ToString(Efficiency));
        }

        // main block of code - runs every physics frame
        public void FixedUpdate()
        {
            frames += 1;
            if (frames == 30)
            {
                SaveNode();
            }
            if (TransmittingTo == null)
            {
                TransmittingTo = " ";
            }
            if (Transmitting == true)
            {
                this.vessel.GetConnectedResourceTotals(EChash, out double amount, out _);
                if (amount<1)
                {
                    powerBeamed = 0;
                }
                // a bunch of math
                excess = Convert.ToSingle(Math.Round((powerBeamed * Efficiency), 1));
                if (Wavelength == "Short")
                {
                    constant = Convert.ToSingle((1.44 * 5 * Math.Pow(10, -8)) / DishDiameter);
                }
                else if (Wavelength == "Long")
                {
                    constant = Convert.ToSingle((1.44 * Math.Pow(10, -3)) / DishDiameter);
                }
                else
                {
                    Debug.Log("Incorrect paramater for wavelength in part.cfg");
                }

                // reducing amount of EC in craft in each frame (makes it look like continuous EC consumption)
                double resource_drain = powerBeamed * Time.fixedDeltaTime;
                this.part.RequestResource(EChash, resource_drain);
            }
            if (Transmitting==false)
            {
                excess = 0;
                powerBeamed = 0;
            }
        }

        public void SaveNode()
        {
            frames = 0;
            string filepath = KSPUtil.ApplicationRootPath + "GameData/BeamedPowerStandalone/PluginData/save.cfg";
            FileInfo info = new FileInfo(filepath);
            ConfigNode BPNode;
            if (info.Length > 0)
            {
                BPNode = ConfigNode.Load(KSPUtil.ApplicationRootPath + "GameData/BeamedPowerStandalone/PluginData/save.cfg");
            }
            else
            {
                BPNode = new ConfigNode();
            }
            string vesselId = Convert.ToString(this.vessel.id); int n2 = 0;
            while (n2 < BPNode.nodes.Count)
            {
                if (BPNode.nodes[n2].GetValue("vesselId") == vesselId)
                {
                    break;
                }
                n2 += 1;
            }
            ConfigNode CorrectNode = new ConfigNode();
            if (n2 == BPNode.nodes.Count)
            {
                BPNode.AddNode("VESSEL", CorrectNode);
            }
            else
            {
                CorrectNode = BPNode.nodes[n2];
            }
            CorrectNode.SetValue("vesselId", vesselId, createIfNotFound: true);
            CorrectNode.SetValue("wavelength", Wavelength, createIfNotFound: true);
            CorrectNode.SetValue("excess", excess, createIfNotFound: true);
            CorrectNode.SetValue("constant", constant, createIfNotFound: true);
            CorrectNode.SetValue("TransmittingTo", "NoVessel", createIfNotFound: true);

            // removes nodes that contain data about vessels that no longer exist
            for (int a = 0; a < BPNode.nodes.Count; a++)
            {
                string vesselId2 = BPNode.nodes[a].GetValue("vesselId"); bool Found = false;
                for (int b = 0; b < FlightGlobals.Vessels.Count; b++)
                {
                    if (vesselId2 == Convert.ToString(FlightGlobals.Vessels[b].id))
                    {
                        Found = true;
                        break;
                    }
                }
                if (Found == false)
                {
                    BPNode.RemoveNode(BPNode.nodes[a]);
                }
            }

            BPNode.Save(KSPUtil.ApplicationRootPath + "GameData/BeamedPowerStandalone/PluginData/save.cfg", "Beamed Power");
            //Saves the confignode to disk.
        }
    }
}
