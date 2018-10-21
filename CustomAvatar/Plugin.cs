﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using IllusionPlugin;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR;

namespace CustomAvatar
{
	public class Plugin : IPlugin
	{
		private const string CustomAvatarsPath = "CustomAvatars";
		private const string FirstPersonEnabledKey = "avatarFirstPerson";
		private const string PreviousAvatarKey = "previousAvatar";
		
		private bool _init;
		private bool _firstPersonEnabled;
        private static bool _isTrackerAsHand;
        private static bool _isFullBodyTracking;

        public static List<XRNodeState> Trackers = new List<XRNodeState>();
        public static bool IsTrackerAsHand
        {
            get { return _isTrackerAsHand; }
            set
            {
                _isTrackerAsHand = value;
                List<XRNodeState> notes = new List<XRNodeState>();
                Trackers = new List<XRNodeState>();
                InputTracking.GetNodeStates(notes);
                foreach (XRNodeState note in notes)
                {
                    if (note.nodeType != XRNode.HardwareTracker || !InputTracking.GetNodeName(note.uniqueID).Contains("LHR-"))
                        continue;
                    Trackers.Add(note);
                }
                if (Trackers.Capacity == 0)
                    _isTrackerAsHand = false;
                Console.WriteLine("IsFullBodyTracking : " + _isFullBodyTracking);
            }
        }

        public static bool IsFullBodyTracking
        {
            get { return _isFullBodyTracking; }
            set
            {
                _isFullBodyTracking = value;
                List<XRNodeState> notes = new List<XRNodeState>();
                Trackers = new List<XRNodeState>();
                InputTracking.GetNodeStates(notes);
                foreach (XRNodeState note in notes)
                {
                    if (note.nodeType != XRNode.HardwareTracker || !InputTracking.GetNodeName(note.uniqueID).Contains("LHR-"))
                        continue;
                    Trackers.Add(note);
                }
                if (Trackers.Capacity == 0)
                    _isFullBodyTracking = false;
                Console.WriteLine("IsFullBodyTracking : " + _isFullBodyTracking);
            }
        }

        private WaitForSecondsRealtime _sceneLoadWait = new WaitForSecondsRealtime(0.1f);
		
		public Plugin()
		{
			Instance = this;
        }

        public event Action<bool> FirstPersonEnabledChanged;

		public static Plugin Instance { get; private set; }
		public AvatarLoader AvatarLoader { get; private set; }
		public PlayerAvatarManager PlayerAvatarManager { get; private set; }

		public bool FirstPersonEnabled
		{
			get { return _firstPersonEnabled; }
			private set
			{
				if (_firstPersonEnabled == value) return;

				_firstPersonEnabled = value;

				if (value)
				{
					PlayerPrefs.SetInt(FirstPersonEnabledKey, 0);
				}
				else
				{
					PlayerPrefs.DeleteKey(FirstPersonEnabledKey);
				}

				if (FirstPersonEnabledChanged != null)
				{
					FirstPersonEnabledChanged(value);
				}
			}
		}

		public string Name
		{
			get { return "Custom Avatars Plugin"; }
		}

		public string Version
		{
			get { return "3.1.3-beta"; }
		}

		public static void Log(string message)
		{
			Console.WriteLine("[CustomAvatarsPlugin] " + message);
			File.AppendAllText("CustomAvatarsPlugin-log.txt", "[Custom Avatars Plugin] " + message + Environment.NewLine);
		}

		public void OnApplicationStart()
		{
			if (_init) return;
			_init = true;
			
			File.WriteAllText("CustomAvatarsPlugin-log.txt", string.Empty);
			
			AvatarLoader = new AvatarLoader(CustomAvatarsPath, AvatarsLoaded);
			
			FirstPersonEnabled = PlayerPrefs.HasKey(FirstPersonEnabledKey);
			SceneManager.sceneLoaded += SceneManagerOnSceneLoaded;
            IsFullBodyTracking = true;
        }

		public void OnApplicationQuit()
		{
			SceneManager.sceneLoaded -= SceneManagerOnSceneLoaded;

			if (PlayerAvatarManager == null) return;
			PlayerAvatarManager.AvatarChanged -= PlayerAvatarManagerOnAvatarChanged;
		}

		private void AvatarsLoaded(IReadOnlyList<CustomAvatar> loadedAvatars)
		{
			if (loadedAvatars.Count == 0)
			{
				Log("No custom avatars found in path " + Path.GetFullPath(CustomAvatarsPath));
				return;
			}

			var previousAvatarPath = PlayerPrefs.GetString(PreviousAvatarKey, null);
			var previousAvatar = AvatarLoader.Avatars.FirstOrDefault(x => x.FullPath == previousAvatarPath);
			
			PlayerAvatarManager = new PlayerAvatarManager(AvatarLoader, previousAvatar);
			PlayerAvatarManager.AvatarChanged += PlayerAvatarManagerOnAvatarChanged;
            IsFullBodyTracking = true;
        }

		private void SceneManagerOnSceneLoaded(Scene newScene, LoadSceneMode mode)
		{
			SharedCoroutineStarter.instance.StartCoroutine(SetCameraCullingMask());
		}

		private void PlayerAvatarManagerOnAvatarChanged(CustomAvatar newAvatar)
		{
			PlayerPrefs.SetString(PreviousAvatarKey, newAvatar.FullPath);
            IsFullBodyTracking = IsFullBodyTracking;
        }

		public void OnUpdate()
		{
			if (Input.GetKeyDown(KeyCode.PageUp))
			{
				if (PlayerAvatarManager == null) return;
				PlayerAvatarManager.SwitchToNextAvatar();
			}
			else if (Input.GetKeyDown(KeyCode.PageDown))
			{
				if (PlayerAvatarManager == null) return;
				PlayerAvatarManager.SwitchToPreviousAvatar();
			}
			else if (Input.GetKeyDown(KeyCode.Home))
			{
				FirstPersonEnabled = !FirstPersonEnabled;
            }
            else if (Input.GetKeyDown(KeyCode.F6))
            {
                IsTrackerAsHand = !IsTrackerAsHand;
            }
            else if (Input.GetKeyDown(KeyCode.F5))
            {
                IsFullBodyTracking = !IsFullBodyTracking;
            }
        }

		private IEnumerator SetCameraCullingMask()
		{
			yield return _sceneLoadWait;
			var mainCamera = Camera.main;
			if (mainCamera == null) yield break;
			mainCamera.cullingMask &= ~(1 << AvatarLayers.OnlyInThirdPerson);
			mainCamera.cullingMask |= 1 << AvatarLayers.OnlyInFirstPerson;
		}

		public void OnFixedUpdate()
		{
		}

		public void OnLevelWasInitialized(int level)
		{
		}

		public void OnLevelWasLoaded(int level)
        {
        }
	}
}