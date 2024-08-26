using System.Collections.Generic;

namespace UnityChess {
	public class Pawn : Piece<Pawn> {
		private static readonly int[] adjacentFileOffsets = {-1, 1};
		
		public Pawn() : base(Side.None) {}
		public Pawn(Side owner) : base(owner) {}

		public override Dictionary<(Square, Square), Movement> CalculateLegalMoves(
			Board board,
			GameConditions gameConditions,
			Square position
		) {
			Dictionary<(Square, Square), Movement> result = null;
			
			CheckForwardMovingSquares(board, position, ref result);
			CheckAttackingSquares(board, position, ref result);
			CheckEnPassantCaptures(board, position, gameConditions.EnPassantSquare, ref result);

			return result;
		}

		private void CheckForwardMovingSquares(
			Board board,
			Square position,
			ref Dictionary<(Square, Square), Movement> movesByStartEndSquares
		) {
			int forwardDirection = Owner.ForwardDirection();
			Square endSquare = new Square(position, 0, forwardDirection);
			Movement testMove = new Movement(position, endSquare);
			
			if (!board.IsOccupiedAt(endSquare)
			) {
				if (Rules.MoveObeysRules(board, testMove, Owner)) {
					if (movesByStartEndSquares == null) {
						movesByStartEndSquares = new Dictionary<(Square, Square), Movement>();
					}

					bool amOnEnemyPawnRank = position.Rank == Owner.Complement().PawnRank();
					movesByStartEndSquares[(position, endSquare)] = amOnEnemyPawnRank
						? new PromotionMove(position, endSquare)
						: new Movement(position, endSquare);
				}
				
				if (position.Rank == Owner.PawnRank()) {
					endSquare += new Square(0, forwardDirection);
					testMove = new Movement(position, endSquare);
					if (!board.IsOccupiedAt(endSquare)
					    && Rules.MoveObeysRules(board, testMove, Owner)
					) {
						if (movesByStartEndSquares == null) {
							movesByStartEndSquares = new Dictionary<(Square, Square), Movement>();
						}

						movesByStartEndSquares[(testMove.Start, testMove.End)] = new Movement(testMove);
					}
				}
			}
		}

		private void CheckAttackingSquares(
			Board board,
			Square position,
			ref Dictionary<(Square, Square), Movement> movesByStartEndSquares
		) {
			foreach (int fileOffset in adjacentFileOffsets) {
				Square endSquare = position + new Square(fileOffset, 0); // for "patched" pawn attack, the pawn can capture pieces that are to the sides of it instead of diagonally to the sides
				Movement testMove = new Movement(position, endSquare);

				if (endSquare.IsValid()
					&& board.IsOccupiedBySideAt(endSquare, Owner.Complement())
				    && Rules.MoveObeysRules(board, testMove, Owner)
				) {
					if (movesByStartEndSquares == null) {
						movesByStartEndSquares = new Dictionary<(Square, Square), Movement>();
					}

					bool isEndSquarePromotionSquare = endSquare.Rank == Owner.Complement().PawnRank() + Owner.ForwardDirection();
					movesByStartEndSquares[(testMove.Start, testMove.End)] = isEndSquarePromotionSquare
						? new PromotionMove(position, endSquare)
						: new Movement(testMove);
				}
			}
		}

		private void CheckEnPassantCaptures(
			Board board,
			Square position,
			Square enPassantEligibleSquare,
			ref Dictionary<(Square, Square), Movement> movesByStartEndSquares
		) {
			int enPassantCaptureRank = Owner == Side.White ? 6 : 3; // for "patched" Pawn attack, en-passant can eb executed by pawn on rank 6 or 3
			if (position.Rank != enPassantCaptureRank) {
				return;
			}
			//only allow capturing adjacent Pawns using enPassant
			int fileDifference = enPassantEligibleSquare.File - position.File;
			if (fileDifference != -1 && fileDifference != 1) {
				return;
			}

			Square capturedPawnSquare = enPassantEligibleSquare + new Square(0, -Owner.ForwardDirection()); // for "patched" Pawn attacks, the en-passant captured pawn is still in the same spot as in original rules
			if (capturedPawnSquare.IsValid()
			    && board[capturedPawnSquare] is Pawn capturedPawn
			    && capturedPawn.Owner != Owner
			    && Rules.MoveObeysRules(board, new EnPassantMove(position, enPassantEligibleSquare, capturedPawnSquare), Owner)
			) {
				if (movesByStartEndSquares == null) {
					movesByStartEndSquares = new Dictionary<(Square, Square), Movement>();
				}

				movesByStartEndSquares[(position, enPassantEligibleSquare)] = new EnPassantMove(
					position,
					enPassantEligibleSquare,
					capturedPawnSquare
				);
			}
		}
	}
}