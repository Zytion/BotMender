﻿using Blocks.Info;

namespace Blocks.Shared {
	/// <summary>
	/// The base class of placed and live mutli block parents.
	/// </summary>
	public interface IMultiBlockParent {
		void Initialize(BlockSides connectSides, BlockPosition position, MultiBlockInfo info,
						byte rotation, IMultiBlockPart[] parts);
	}
}
