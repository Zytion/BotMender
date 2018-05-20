﻿using System.Collections.Generic;
using UnityEngine;
using Assets.Scripts.Blocks;
using Assets.Scripts.Blocks.Info;
using Assets.Scripts.Blocks.Placed;
using Assets.Scripts.Playing;
using Assets.Scripts.Structures;

namespace Assets.Scripts.Building {
	/// <summary>
	/// Allows the player to interact with the structure the script is attached to, should be used in build mode.
	/// </summary>
	public class BuildingController : MonoBehaviour {
		private readonly HashSet<RealPlacedBlock> _previousNotConnected = new HashSet<RealPlacedBlock>();
		private Camera _camera;
		private EditableStructure _structure;
		private int _blockType;
		private byte _facingVariant;
		private BlockPosition _previousPreviewPosition;
		private GameObject _previewObject;

		public void Awake() {
			_camera = Camera.main;
			_structure = GetComponent<EditableStructure>();
		}



		public void Update() {
			Rotate(Input.GetAxisRaw("MouseScroll"));
			if (Input.GetButtonDown("Fire3")) {
				Switch();
			}

			if (Input.GetButtonDown("Fire1")) {
				Place();
			} else if (Input.GetButtonDown("Fire2")) {
				Delete();
			}

			if (Input.GetButtonDown("Ability")) {
				IDictionary<BlockPosition, IPlacedBlock> notConnected = _structure.GetNotConnectedBlocks();
				if (notConnected == null || notConnected.Count != 0) {
					Debug.Log("Invalid structure: " + (notConnected == null ? "no mainframe" : "not connected blocks"));
				} else {
					CompleteStructure complete = CompleteStructure.Create(_structure.Serialize());
					if (complete == null) {
						Debug.Log("Failed to create CompleteStructure");
					} else {
						complete.gameObject.AddComponent<HumanBotController>();
						_camera.gameObject.AddComponent<PlayingCameraController>()
							.Structure = complete.GetComponent<Rigidbody>();
						Destroy(_camera.gameObject.GetComponent<BuildingCameraController>());
						Destroy(gameObject);
					}
				}
			}
		}

		public void FixedUpdate() {
			GameObject block;
			BlockPosition position;
			byte rotation;
			if (GetSelectedBlock(out block, out position, out rotation) && !position.Equals(_previousPreviewPosition)) {
				ShowPreview(position, rotation);
			}
		}



		private void Rotate(float rawInput) {
			if (rawInput > 0) {
				_facingVariant++;
			} else if (rawInput < 0) {
				_facingVariant--;
			}
			ShowPreview();
		}

		private void Switch() {
			_blockType = (_blockType + 1) % BlockFactory.TypeCount;
			ShowPreview();
			Debug.Log("Switched to: " + BlockFactory.GetType(_blockType));
		}

		private void Place() {
			GameObject block;
			BlockPosition position;
			byte rotation;
			if (!GetSelectedBlock(out block, out position, out rotation)) {
				return;
			}

			BlockInfo info = BlockFactory.GetInfo(BlockFactory.GetType(_blockType));
			if (_structure.TryAddBlock(position, info, rotation)) {
				ColorNotConnectedBlocks();
				ShowPreview(position, rotation);
			}
		}

		private void Delete() {
			GameObject block;
			BlockPosition position;
			byte rotation;
			if (!GetSelectedBlock(out block, out position, out rotation)) {
				return;
			}
			
			RealPlacedBlock component = block.GetComponent<RealPlacedBlock>();
			if (component != null) {
				_structure.RemoveBlock(component.Position);
				ColorNotConnectedBlocks();
				ShowPreview(position, rotation);
			}
		}



		private void ShowPreview() {
			GameObject block;
			BlockPosition position;
			byte rotation;
			if (GetSelectedBlock(out block, out position, out rotation)) {
				ShowPreview(position, rotation);
			}
		}
		
		private void ShowPreview(BlockPosition position, byte rotation) {
			Destroy(_previewObject);
			_previousPreviewPosition = position;
			BlockInfo info = BlockFactory.GetInfo(BlockFactory.GetType(_blockType));
			if (!_structure.CanAddBlock(position, info, rotation)) {
				return;
			}

			SingleBlockInfo single = info as SingleBlockInfo;
			RealPlacedBlock block;
			if (single != null) {
				block = BlockFactory.MakeSinglePlaced(_structure.transform, single, rotation, position);
			} else {
				PlacedMultiBlockPart[] parts;
				block = BlockFactory.MakeMultiPlaced(_structure.transform, (MultiBlockInfo)info, rotation, position, out parts);
			}

			_previewObject = block.gameObject;
			_previewObject.gameObject.name = "BlockPreview";
			DestroyImmediate(_previewObject.GetComponent<Collider>());
			Renderer render = _previewObject.GetComponent<Renderer>();
			EnableMaterialTransparency(render.material);
			render.material.color = new Color(1, 1, 1, 0.5f);
		}

		private static void EnableMaterialTransparency(Material material) {
			material.SetInt("_Mode", 3);
			material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
			material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
			material.SetInt("_ZWrite", 0);
			material.DisableKeyword("_ALPHATEST_ON");
			material.DisableKeyword("_ALPHABLEND_ON");
			material.EnableKeyword("_ALPHAPREMULTIPLY_ON");
			material.renderQueue = 3000;
		}



		private bool GetSelectedBlock(out GameObject block, out BlockPosition position, out byte rotation) {
			block = null;
			position = null;
			rotation = 0;
			RaycastHit hit;
			if (!Physics.Raycast(_camera.transform.position, _camera.transform.forward, out hit)
				|| !BlockPosition.FromVector(hit.point + hit.normal / 2, out position)) {
				return false;
			}
			rotation = Rotation.GetByte(BlockSide.FromNormal(hit.normal), _facingVariant);
			block = hit.transform.gameObject;
			return true;
		}



		private void ColorNotConnectedBlocks() {
			foreach (RealPlacedBlock block in _previousNotConnected) {
				block.GetComponent<Renderer>().material.color = Color.white;
			}
			_previousNotConnected.Clear();

			IDictionary<BlockPosition, IPlacedBlock> notConnected = _structure.GetNotConnectedBlocks();
			if (notConnected == null) {
				return;
			}

			foreach (IPlacedBlock block in notConnected.Values) {
				RealPlacedBlock real = block as RealPlacedBlock;
				if (real != null) {
					real.GetComponent<Renderer>().material.color = Color.red;
					_previousNotConnected.Add(real);
				}
			}
		}
	}
}
