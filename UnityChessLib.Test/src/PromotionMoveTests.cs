using NUnit.Framework;

namespace UnityChess.Test {
	[TestFixture]
	public class PromotionMoveTests {
		private Board board;

		[SetUp]
		public void Init() {
			board = new Board();
			board.ClearBoard();
		}

		[Test]
		[TestCase(ElectedPiece.Knight)]
		[TestCase(ElectedPiece.Bishop)]
		[TestCase(ElectedPiece.Rook)]
		[TestCase(ElectedPiece.Queen)]
		public void HandleAssociatedPiece_PromotionMove_ElectedPieceGenerated(ElectedPiece election) {
			Square expectedPosition = new Square(1, 8);
			Movement movePromotionMove = Movement.PromotionMove(Square.Invalid, expectedPosition);
			movePromotionMove.SetPromotionPiece(PromotionUtil.GeneratePromotionPiece(election, Side.White));

			movePromotionMove.HandleAssociatedPiece(board);

			Assert.AreEqual(election.ToString(), board[expectedPosition]?.GetType().Name);
		}
	}
}