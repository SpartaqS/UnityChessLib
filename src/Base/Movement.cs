namespace UnityChess {
	/// <summary>Representation of a move, namely a piece and its end square.</summary>
	public struct Movement {
		public readonly Square Start;
		public readonly Square End;

		/// <summary>Creates a new Movement.</summary>
		/// <param name="piecePosition">Position of piece being moved.</param>
		/// <param name="end">Square which the piece will land on.</param>
		public Movement(Square piecePosition, Square end) : this(piecePosition, end, new Square(-1,-1), MoveType.NormalMove) { } //TEMP just to keep track of this constructor as it was used

		/// <summary>Copy constructor.</summary>
		internal Movement(Movement move) : this(move.Start, move.End) { }

		public bool Equals(Movement other) {
			return Start == other.Start && End == other.End && SpecialMoveSquare == other.SpecialMoveSquare && Type == other.Type && PromotionPiece == other.PromotionPiece; 
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
		public readonly MoveType Type;
		public readonly bool IsSpecialMove => Type != MoveType.NormalMove;
		public readonly Square SpecialMoveSquare;

		public Movement(Square piecePosition, Square end, Square specialMoveSquare, MoveType moveType)
		{
			Start = piecePosition;
			End = end;
			SpecialMoveSquare = specialMoveSquare;
			// set special move type based on needs
			Type = moveType;
			PromotionPiece = ElectedPiece.None;
			PromotionPieceSide = Side.None;
		}

		public void HandleAssociatedPiece(Board board) {
			if (IsCastlingMove)
			{
				HandleAssociatedPieceCastlingMove(board);
			}
			else if (IsEnPassantMove)
			{
				HandleAssociatedPieceEnPassant(board);
			}
			else if (IsPromotionMove)
			{
				HandleAssociatedPiecePromotionMove(board);
			}

			else if (IsSpecialMove)
			{
				throw new System.Exception("HandleAssociatedPiece called without specifying what kind of special move it is");
			}
			else if (!IsSpecialMove)
			{
				throw new System.Exception("HandleAssociatedPiece called on a non-special move");
			}
		}

		public enum MoveType { 
			NormalMove,
			CastlingMove,
			EnPassantMove,
			PromotionMove
		}


		#region CastlingMove
		/// <summary>Creates a new CastlingMove instance.</summary>
		/// <param name="kingPosition">Position of the king to be castled.</param>
		/// <param name="end">Square on which the king will land on.</param>
		/// <param name="rookSquare">The square of the rook associated with the castling move.</param>
		public static Movement CastlingMove(Square kingPosition, Square end, Square rookSquare)
		{
			return new Movement(kingPosition, end, rookSquare, MoveType.CastlingMove);
		}

		public readonly bool IsCastlingMove => Type == MoveType.CastlingMove;

		public readonly Square RookSquare => SpecialMoveSquare;

		/// <summary>Handles moving the associated rook to the correct position on the board.</summary>
		/// <param name="board">Board on which the move is being made.</param>
		private void HandleAssociatedPieceCastlingMove(Board board)
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

		#region EnPassantMove


		/// <summary>Creates a new EnPassantMove instance; inherits from SpecialMove.</summary>
		/// <param name="attackingPawnPosition">Position of the attacking pawn.</param>
		/// <param name="end">Square on which the attacking pawn will land on.</param>
		/// <param name="capturedPawnSquare">Square of the pawn that is being captured via en passant.</param>
		public static Movement EnPassantMove(Square attackingPawnPosition, Square end, Square capturedPawnSquare)
		{
			return new Movement(attackingPawnPosition, end, capturedPawnSquare, MoveType.EnPassantMove);
		}

		public readonly bool IsEnPassantMove => Type == MoveType.EnPassantMove;

		public readonly Square CapturedPawnSquare => SpecialMoveSquare;

		private void HandleAssociatedPieceEnPassant(Board board)
		{
			board[CapturedPawnSquare] = null;
		}

		#endregion

		#region PromotionMove

		public ElectedPiece PromotionPiece { get; private set; }
		public Side PromotionPieceSide { get; private set; }

		/// <summary>Creates a new PromotionMove instance; inherits from SpecialMove.</summary>
		/// <param name="pawnPosition">Position of the promoting pawn.</param>
		/// <param name="end">Square which the promoting pawn is landing on.</param>
		public static Movement PromotionMove(Square pawnPosition, Square end)
		{
			return new Movement(pawnPosition, end, end, MoveType.PromotionMove);
		}

		public readonly bool IsPromotionMove => Type == MoveType.PromotionMove;


		/// <summary>Handles replacing the promoting pawn with the elected promotion piece.</summary>
		/// <param name="board">Board on which the move is being made.</param>
		private void HandleAssociatedPiecePromotionMove(Board board)
		{
			if (PromotionPiece == ElectedPiece.None)
			{
				throw new System.ArgumentNullException(
					$"{nameof(HandleAssociatedPiece)}:\n"
					+ $"{nameof(PromotionMove)}.{nameof(PromotionPiece)} was null.\n"
					+ $"You must first call {nameof(PromotionMove)}.{nameof(SetPromotionPiece)}"
					+ $" before it can be executed."
				);
			}

			board[End] = PromotionUtil.GeneratePromotionPiece(PromotionPiece,PromotionPieceSide);
		}

		public void SetPromotionPiece(Piece promotionPiece)
		{
			PromotionPiece = PromotionUtil.ReadElectedPieceFromPiece(promotionPiece);
			PromotionPieceSide = promotionPiece.Owner;
		}

		#endregion

		public static Movement InvalidMove()
		{
			return new Movement(Square.Invalid, Square.Invalid, Square.Invalid, MoveType.NormalMove);
		}

		/// <summary>
		/// basic check if move is valid (enough to determine whether to discard it or not)
		/// Special moves do have a special move square but regular moves do not use the special move square
		/// </summary>
		/// <returns></returns>
		public bool IsValid()
		{
			return Start.IsValid() && End.IsValid() && (!IsSpecialMove || SpecialMoveSquare.IsValid());
		}

		public static bool operator ==(Movement m1, Movement m2)
		{
			return m1.Equals(m2);
		}

		public static bool operator !=(Movement m1, Movement m2)
		{
			return !m1.Equals(m2);
		}
	}

	public class MovementHolderClass
	{
		public Movement StoredMovement;

		public MovementHolderClass(Movement movement)
		{
			this.StoredMovement = movement;
		}
	}


}