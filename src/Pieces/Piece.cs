using System;
using System.Collections.Generic;

namespace UnityChess {
	/// <summary>Base class for any chess piece.</summary>
	public abstract class Piece {
		public Side Owner { get; protected set; }

		protected Piece(Side owner) {
			Owner = owner;
		}

		public abstract Piece DeepCopy();

		public abstract Dictionary<(Square, Square), Movement> CalculateLegalMoves(
			Board board,
			GameConditions gameConditions,
			Square position
		);
		
		public override string ToString() => $"{Owner} {GetType().Name}";

		public string ToTextArt() => this switch {
			Bishop { Owner: Side.White } => "♝",
			Bishop { Owner: Side.Black } => "♗",
			King { Owner: Side.White } => "♚",
			King { Owner: Side.Black } => "♔",
			Knight { Owner: Side.White } => "♞",
			Knight { Owner: Side.Black } => "♘",
			Queen { Owner: Side.White } => "♛",
			Queen { Owner: Side.Black } => "♕",
			Pawn { Owner: Side.White } => "♟",
			Pawn { Owner: Side.Black } => "♙",
			Rook { Owner: Side.White } => "♜",
			Rook { Owner: Side.Black } => "♖",
			_ => "."
		};

		public override bool Equals(System.Object obj)
		{
			var other = obj as Piece;
			if (other == null)
			{
				return false;
			}

			return Owner == other.Owner && GetType().Name == other.GetType().Name;
		}

		public override int GetHashCode()
		{
			return HashCode.Combine(Owner, GetType().Name);
		}

		static public bool operator ==(Piece lPiece, Piece rPiece)
		{
			if (ReferenceEquals(lPiece, null))
			{
				if (ReferenceEquals(rPiece, null))
				{
					// null == null
					return true;
				}
				// only left side is null
				return false;
			}
			// Equals() haldes rBoard == null
			return lPiece.Equals(rPiece);
		}

		static public bool operator !=(Piece lPiece, Piece rPiece)
		{
			return !(lPiece == rPiece);
		}
	}

	public abstract class Piece<T> : Piece where T : Piece<T>, new() {
		protected Piece(Side owner) : base(owner) { }
		
		public override Piece DeepCopy() {
			return new T {
				Owner = Owner
			};
		}
	}
}