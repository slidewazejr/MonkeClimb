using BepInEx;
using System;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;
using System.Reflection;
using GorillaLocomotion;
using System.Collections.Generic;
using UnityEngine.XR;
using Photon.Pun;
using System.IO;


namespace SpiderMonke
{
    [BepInPlugin("org.Charlie.gorilla.climb", "Monke Climb", "1.0.0.0")]
    public class HarmonyStuff : BaseUnityPlugin
    {
        public void Awake()
        {
            var harmony = new Harmony("com.BananaInc.gorilla.climb");
            harmony.PatchAll(Assembly.GetExecutingAssembly());
        }
    }

    [HarmonyPatch(typeof(GorillaLocomotion.Player))]
    [HarmonyPatch("Update")]

    public class WallGrab
    {
        private static int layers = (1 << 9);

        private static bool Onstart = false;

        private static float RG;
        private static float LG;

        private static Vector3 CurHandPos;
        private static bool RGrabbing;
        private static bool LGrabbing;

        private static ConfigEntry<bool> InAir;
        private static bool AirMode;
        private static void Postfix(GorillaLocomotion.Player __instance)
        {
            if (!Onstart)
            {
                var file = new ConfigFile(Path.Combine(Paths.ConfigPath, "MonkeClimb.cfg"), true);
                InAir = file.Bind("Monke Climb Settings", "Air-Mode", false, "Allows you to grab the air as well");
                AirMode = InAir.Value;
                Onstart = true;
            }

            if (!PhotonNetwork.InRoom || !PhotonNetwork.CurrentRoom.IsVisible)
            {
                List<InputDevice> list = new List<InputDevice>();
                InputDevices.GetDevices(list);

                for (int i = 0; i < list.Count; i++) //Get input
                {
                    if (list[i].characteristics.HasFlag(InputDeviceCharacteristics.Left))
                    {
                        list[i].TryGetFeatureValue(CommonUsages.grip, out LG);
                    }
                    if (list[i].characteristics.HasFlag(InputDeviceCharacteristics.Right))
                    {
                        list[i].TryGetFeatureValue(CommonUsages.grip, out RG);
                    }
                }

                RaycastHit Lray;

                var leftR = Physics.Raycast(__instance.leftHandTransform.position, __instance.leftHandTransform.right, out Lray, 0.2f, layers) ;

                RaycastHit Rray;

                var RightR = Physics.Raycast(__instance.rightHandTransform.position, -__instance.rightHandTransform.right, out Lray, 0.2f, layers);

                if ((RightR || AirMode) && RG > 0.5f)
                {
                    if (!RGrabbing)
                    {
                        CurHandPos = __instance.rightHandTransform.position;
                        RGrabbing = true;
                        LGrabbing = false;
                    }

                    var pos = __instance.rightHandTransform.position;

                    ApplyVelocity(pos, CurHandPos, __instance);
                }
                else if ((leftR || AirMode) && LG > 0.5f)
                {
                    if (!LGrabbing)
                    {
                        CurHandPos = __instance.leftHandTransform.position;
                        LGrabbing = true;
                        RGrabbing = false;
                    }

                    var pos = __instance.leftHandTransform.position;

                    ApplyVelocity(pos, CurHandPos, __instance);
                }
                else
                {
                    if(LGrabbing || RGrabbing)
                    {
                        __instance.bodyCollider.attachedRigidbody.useGravity = true;
                        LGrabbing = false;
                        RGrabbing = false;
                    }
                }
            }
        }

        public static void ApplyVelocity(Vector3 pos, Vector3 target, GorillaLocomotion.Player __instance)
        {
            __instance.bodyCollider.attachedRigidbody.useGravity = false;
            var dir = target - pos;
            __instance.bodyCollider.attachedRigidbody.velocity = dir * 65;
        }
    }
}
