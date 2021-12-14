﻿using System.Collections.Generic;

namespace UnityChess {
	/// <summary>Representation of a standard chess game including a history of moves made.</summary>
	public class Game {
		public Mode Mode { get; }
		public Side SideToMove { get; private set; }
		public int LatestHalfMoveIndex => HalfMoveTimeline.HeadIndex;
		public Timeline<GameConditions> ConditionsTimeline { get; }
		public Timeline<Board> BoardTimeline { get; }
		public Timeline<HalfMove> HalfMoveTimeline { get; }
		public Timeline<Dictionary<Piece, Dictionary<(Square, Square), Movement>>> LegalMovesTimeline { get; }

		/// <summary>Creates a Game instance of a given mode with a standard starting Board.</summary>
		/// <param name="mode">Describes which players are human or AI.</param>
		/// <param name="startingConditions">Conditions at the time the board was set up.</param>
		public Game(Mode mode, GameConditions startingConditions) : this(mode, startingConditions, Board.GetStartingPositionPieces()) { }

		public Game(Mode mode, GameConditions startingConditions, params Piece[] pieces) {
			Mode = mode;
			SideToMove = Side.White;

			BoardTimeline = new Timeline<Board> { new Board(pieces) };
			HalfMoveTimeline = new Timeline<HalfMove>();
			ConditionsTimeline = new Timeline<GameConditions> { startingConditions };
			LegalMovesTimeline = new Timeline<Dictionary<Piece, Dictionary<(Square, Square), Movement>>> {
				CalculateLegalMovesForPosition(BoardTimeline.Current, ConditionsTimeline.Current)
			};
		}

		/// <summary>Executes passed move and switches sides; also adds move to history.</summary>
		public bool TryExecuteMove(Movement move) {
			if (!TryGetLegalMove(move.Start, move.End, out Movement validatedMove)) {
				return false;
			}

			//create new copy of previous current board, and execute the move on it
			Board boardBeforeMove = BoardTimeline.Current;
			Board resultingBoard = new Board(boardBeforeMove);
			resultingBoard.MovePiece(validatedMove);
			BoardTimeline.AddNext(resultingBoard);

			SideToMove = SideToMove.Complement();
			
			bool capturedPiece = boardBeforeMove[validatedMove.End] != null || validatedMove is EnPassantMove;
			bool causedCheck = Rules.IsPlayerInCheck(resultingBoard, SideToMove);
			
			HalfMove halfMove = new HalfMove(boardBeforeMove[validatedMove.Start], validatedMove, capturedPiece, causedCheck);
			GameConditions resultingGameConditions = ConditionsTimeline.Current.CalculateEndingConditions(boardBeforeMove, halfMove);
			ConditionsTimeline.AddNext(resultingGameConditions);

			Dictionary<Piece, Dictionary<(Square, Square), Movement>> legalMovesByPiece
				= CalculateLegalMovesForPosition(resultingBoard, resultingGameConditions);

			int numLegalMoves = GetNumLegalMoves(legalMovesByPiece);

			LegalMovesTimeline.AddNext(legalMovesByPiece);

			halfMove.SetGameEndBools(
				Rules.IsPlayerStalemated(resultingBoard, SideToMove, numLegalMoves),
				Rules.IsPlayerCheckmated(resultingBoard, SideToMove, numLegalMoves)
			);
			HalfMoveTimeline.AddNext(halfMove);
			
			return true;
		}

		public bool TryGetLegalMove(Square startSquare, Square endSquare, out Movement move) {
			move = null;

			return BoardTimeline.Current[startSquare] is { } movingPiece
			       && LegalMovesTimeline.Current.TryGetValue(movingPiece, out Dictionary<(Square, Square), Movement> movesByStartEndSquares)
			       && movesByStartEndSquares.TryGetValue((startSquare, endSquare), out move);
		}
		
		public bool TryGetLegalMovesForPiece(Piece movingPiece, out ICollection<Movement> legalMoves) {
			legalMoves = null;

			if (movingPiece != null
			    && LegalMovesTimeline.Current.TryGetValue(
				    movingPiece,
				    out Dictionary<(Square, Square), Movement> movesByStartEndSquares
			    )
			    && movesByStartEndSquares != null
			) {
				legalMoves = movesByStartEndSquares.Values;
				return true;
			}

			return false;
		}

		public bool ResetGameToHalfMoveIndex(int halfMoveIndex) {
			if (LatestHalfMoveIndex == -1) return false; // i.e. No possible move to reset to

			BoardTimeline.HeadIndex = halfMoveIndex + 1;
			ConditionsTimeline.HeadIndex = halfMoveIndex + 1;
			LegalMovesTimeline.HeadIndex = halfMoveIndex + 1;
			HalfMoveTimeline.HeadIndex = halfMoveIndex;
			SideToMove = halfMoveIndex % 2 == 0 ? Side.Black : Side.White;
			
			return true;
		}
		
		internal static int GetNumLegalMoves(Dictionary<Piece, Dictionary<(Square, Square), Movement>> legalMovesByPiece) {
			int result = 0;
			
			if (legalMovesByPiece != null) {
				foreach (Dictionary<(Square, Square), Movement> movesByStartEndSquares in legalMovesByPiece.Values) {
					result += movesByStartEndSquares.Count;
				}
			}

			return result;
		}
		
		internal static Dictionary<Piece, Dictionary<(Square, Square), Movement>> CalculateLegalMovesForPosition(
			Board board,
			GameConditions gameConditions
		) {
			Dictionary<Piece, Dictionary<(Square, Square), Movement>> result = null;
			
			for (int file = 1; file <= 8; file++) {
				for (int rank = 1; rank <= 8; rank++) {
					if (board[file, rank] is Piece piece
					    && piece.Owner == gameConditions.SideToMove
					    && piece.CalculateLegalMoves(board, gameConditions, new Square(file, rank)) is
						    { } movesByStartEndSquares
					) {
						if (result == null) {
							result = new Dictionary<Piece, Dictionary<(Square, Square), Movement>>();
						}

						result[piece] = movesByStartEndSquares;
					}
				}
			}

			return result;
		}
	}
}