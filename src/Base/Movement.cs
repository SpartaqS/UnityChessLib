namespace UnityChess {
	/// <summary>Representation of a move, namely a piece and its end square.</summary>
	public struct Movement {
		public readonly Square Start;
		public readonly Square End;

		/// <summary>Creates a new Movement.</summary>
		/// <param name="piecePosition">Position of piece being moved.</param>
		/// <param name="end">Square which the piece will land on.</param>
		public Movement(Square piecePosition, Square end) : this(piecePosition, end, new Square(-1,-1), false) { } //TEMP just to keep track of this constructor as it was used

		/// <summary>Copy constructor.</summary>
		internal Movement(Movement move) : this(move.Start, move.End) { }

		public bool Equals(Movement other) {
			return Start == other.Start && End == other.End; 
		}

		public override bool Equals(object obj) {
			if (ReferenceEquals(null, obj)) return false;
			if (ReferenceEquals(this, obj)) return true;
			return GetType() == obj.GetType() && Equals((Movement) obj);
		}

		public override int GetHashCode() {
			unchecked {
				return (Start.GetHashCode() * 397) ^ End.GetHashCode();
			}
		}

		public override string ToString() => $"{Start}->{End}";

		// results of converting class to struct:
		public readonly bool IsSpecialMove;

		public Movement(Square piecePosition, Square end, Square specialMoveSquare, bool isSpecialMove = false, bool isCastlingMove = false)
		{
			Start = piecePosition;
			End = end;
			IsSpecialMove = isSpecialMove;
			SpecialMoveSquare = specialMoveSquare;
			// set special move type based on needs
			IsCastlingMove = isCastlingMove;
		}

		public void HandleAssociatedPiece(Board board) { 
			if (IsCastlingMove)
			{
				HandleAssociatedPieceCastlingMove(board);
			}
			else if (IsSpecialMove)
			{

			}
			throw new System.Exception("HandleAssociatedPiece called without specifying what kind of special move it is");
		}

		#region CastlingMove
		public static Movement CastlingMove(Square kingPosition, Square end, Square rookSquare)
		{
			return new Movement(kingPosition, end, rookSquare, true, true);
		}

		public readonly bool IsCastlingMove;
		public readonly Square SpecialMoveSquare;
		public readonly Square RookSquare => SpecialMoveSquare;
		public void HandleAssociatedPieceCastlingMove(Board board)
		{
			if (board[RookSquare] is Rook rook)
			{
				board[RookSquare] = null;
				board[GetRookEndSquare()] = rook;
			}
			else
			{
				throw new System.ArgumentException(
					$"{nameof(CastlingMove)}.{nameof(HandleAssociatedPiece)}:\n"
					+ $"No {nameof(Rook)} found at {nameof(RookSquare)}"
				);
			}
		}

		public Square GetRookEndSquare()
		{
			int rookFileOffset = RookSquare.File switch
			{
				1 => 3,
				8 => -2,
				_ => throw new System.ArgumentException(
					$"{nameof(RookSquare)}.{nameof(RookSquare.File)} is invalid"
				)
			};

			return RookSquare + new Square(rookFileOffset, 0);
		}

		#endregion
	}
}