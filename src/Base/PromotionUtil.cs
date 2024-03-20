namespace UnityChess {
	public static class PromotionUtil {
		public static Piece GeneratePromotionPiece(ElectedPiece election, Side side) => election switch {
			ElectedPiece.Bishop => new Bishop(side),
			ElectedPiece.Knight => new Knight(side),
			ElectedPiece.Queen => new Queen(side),
			ElectedPiece.Rook => new Rook(side),
			_ => null
		};

		public static ElectedPiece ReadElectedPieceFromPiece(Piece election) => election switch
		{
			Bishop => ElectedPiece.Bishop,
			Knight => ElectedPiece.Knight,
			Queen => ElectedPiece.Queen,
			Rook => ElectedPiece.Rook,
			_ => ElectedPiece.None
		};
	}
}
