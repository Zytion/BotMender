﻿using System;
using System.Collections.Generic;
using Networking;
using UnityEngine;

namespace Utilities {
	/// <summary>
	/// A utility class used to queue actions to be invoked on the main Unity thread.
	/// </summary>
	public class UnityDispatcher : MonoBehaviour {
		private static UnityDispatcher _instance;
		private readonly Queue<Action> _actions = new Queue<Action>();

		private void Awake() {
			DontDestroyOnLoad(this);
			lock (_actions) {
				_instance = this;
			}
		}

		private void OnDestroy() {
			lock (_actions) {
				_instance = null;
			}
		}



		/// <summary>
		/// Queues the specfied action to be invoked on the main Unity thread.
		/// Depending on current (hardcoded) settings, delay may be applied
		/// when using this method in order to simulate latency.
		/// </summary>
		public static void InvokePacketHandling(bool udp, Action action) {
			lock (_instance._actions) {
				if (NetworkUtils.SimulateUdpNetworkConditions) {
					_instance._actions.Enqueue(() => {
						if (!udp || !NetworkUtils.ShouldLoseUdpPacket) {
							_instance.StartCoroutine(CoroutineUtils.Delay(action, NetworkUtils.SimulatedLatency));
						}
					});
				} else {
					_instance._actions.Enqueue(action);
				}
			}
		}

		/// <summary>
		/// Queues the specfied action to be invoked on the main Unity thread.
		/// Tries to add no additional delay.
		/// </summary>
		public static void InvokeNoDelay(Action action) {
			lock (_instance._actions) {
				_instance._actions.Enqueue(action);
			}
		}



		private void Update() {
			CheckInvokables();
		}

		private void FixedUpdate() {
			CheckInvokables();
		}

		private void CheckInvokables() {
			while (true) {
				Action action;
				lock (_actions) {
					if (_actions.Count != 0) {
						action = _actions.Dequeue();
					} else {
						return;
					}
				}
				action();
			}
		}
	}
}
