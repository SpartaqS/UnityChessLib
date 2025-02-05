﻿using System.Collections.Generic;

namespace UnityChess {
	/// <summary>Representation of a standard chess game including a history of moves made.</summary>
	public class Game {
		public Timeline<GameConditions> ConditionsTimeline { get; }
		public Timeline<Board> BoardTimeline { get; }
		public Timeline<HalfMove> HalfMoveTimeline { get; }
		public Timeline<Dictionary<Piece, Dictionary<(Square, Square), Movement>>> LegalMovesTimeline { get; }

		/// <summary>Creates a Game instance of a given mode with a standard starting Board.</summary>
		public Game() : this(GameConditions.NormalStartingConditions, Board.StartingPositionPieces) { }

		public Game(GameConditions startingConditions, params (Square, Piece)[] squarePiecePairs) : this(startingConditions, new Board(squarePiecePairs)) {	}

		public Game(GameConditions startingConditions, Board startingBoard)
		{
			Board newStartingBoard = new Board(startingBoard);
			BoardTimeline = new Timeline<Board> { newStartingBoard };
			HalfMoveTimeline = new Timeline<HalfMove>();
			ConditionsTimeline = new Timeline<GameConditions> { startingConditions };
			LegalMovesTimeline = new Timeline<Dictionary<Piece, Dictionary<(Square, Square), Movement>>> {
				CalculateLegalMovesForPosition(newStartingBoard, startingConditions)
			};
		}

		/// <summary>
		/// Creates shallow copies of all timelines
		/// </summary>
		/// <param name="gameToCopy"></param>
		public Game(Game gameToCopy)
		{
			BoardTimeline = new Timeline<Board>(gameToCopy.BoardTimeline);
			HalfMoveTimeline = new Timeline<HalfMove>(gameToCopy.HalfMoveTimeline);
			ConditionsTimeline = new Timeline<GameConditions>(gameToCopy.ConditionsTimeline);
			LegalMovesTimeline = new Timeline<Dictionary<Piece, Dictionary<(Square, Square), Movement>>>(gameToCopy.LegalMovesTimeline);
		}

		/// <summary>Executes passed move and switches sides; also adds move to history.</summary>
		public bool TryExecuteMove(Movement move) {
			if (!TryGetLegalMove(move.Start, move.End, out Movement validatedMove)) {
				return false;
			}

			// if the SpecialMove was deemed valid, save its special properties
			if (validatedMove is SpecialMove)
				validatedMove = move;

			//create new copy of previous current board, and execute the move on it
			BoardTimeline.TryGetCurrent(out Board boardBeforeMove);
			Board resultingBoard = new Board(boardBeforeMove);
			resultingBoard.MovePiece(validatedMove);
			BoardTimeline.AddNext(resultingBoard);
			
			ConditionsTimeline.TryGetCurrent(out GameConditions conditionsBeforeMove); 
			Side updatedSideToMove = conditionsBeforeMove.SideToMove.Complement();
			bool causedCheck = Rules.IsPlayerInCheck(resultingBoard, updatedSideToMove);
			bool capturedPiece = boardBeforeMove[validatedMove.End] != null || validatedMove is EnPassantMove;

			bool causedThreeFoldRepetition = Rules.IsThreefoldRepetition(this);

			HalfMove halfMove = new HalfMove(boardBeforeMove[validatedMove.Start], validatedMove, capturedPiece, causedCheck);
			GameConditions resultingGameConditions = conditionsBeforeMove.CalculateEndingConditions(boardBeforeMove, halfMove);

			ConditionsTimeline.AddNext(resultingGameConditions);

			bool caused50MovesDraw = Rules.Is50MovesDraw(resultingGameConditions);

			Dictionary<Piece, Dictionary<(Square, Square), Movement>> legalMovesByPiece
				= causedThreeFoldRepetition || caused50MovesDraw ? null : CalculateLegalMovesForPosition(resultingBoard, resultingGameConditions);

			int numLegalMoves = GetNumLegalMoves(legalMovesByPiece);

			LegalMovesTimeline.AddNext(legalMovesByPiece);

			halfMove.SetGameEndBools(
				Rules.IsPlayerStalemated(resultingBoard, updatedSideToMove, numLegalMoves),
				Rules.IsPlayerCheckmated(resultingBoard, updatedSideToMove, numLegalMoves),
				causedThreeFoldRepetition,
				caused50MovesDraw
			);
			HalfMoveTimeline.AddNext(halfMove);
			
			return true;
		}

		public bool TryGetLegalMove(Square startSquare, Square endSquare, out Movement move) {
			move = null;

			return BoardTimeline.TryGetCurrent(out Board currentBoard)
			       && LegalMovesTimeline.TryGetCurrent(out Dictionary<Piece, Dictionary<(Square, Square), Movement>> currentLegalMoves)
			       && currentBoard[startSquare] is { } movingPiece
			       && currentLegalMoves.TryGetValue(movingPiece, out Dictionary<(Square, Square), Movement> movesByStartEndSquares)
			       && movesByStartEndSquares.TryGetValue((startSquare, endSquare), out move);
		}
		
		public bool TryGetLegalMovesForPiece(Piece movingPiece, out ICollection<Movement> legalMoves) {
			legalMoves = null;

			if (movingPiece != null
			    && LegalMovesTimeline.TryGetCurrent(out Dictionary<Piece, Dictionary<(Square, Square), Movement>> legalMovesByPiece)
			    && legalMovesByPiece.TryGetValue(movingPiece, out Dictionary<(Square, Square), Movement> movesByStartEndSquares)
			    && movesByStartEndSquares != null
			) {
				legalMoves = movesByStartEndSquares.Values;
				return true;
			}

			return false;
		}

		public bool ResetGameToHalfMoveIndex(int halfMoveIndex) {
			if (HalfMoveTimeline.HeadIndex == -2) { // changed from -1 to -2 so AI_MinMax can step back after the first move of the evaluated board
				return false;
			}

			BoardTimeline.HeadIndex = halfMoveIndex + 1;
			ConditionsTimeline.HeadIndex = halfMoveIndex + 1;
			LegalMovesTimeline.HeadIndex = halfMoveIndex + 1;
			HalfMoveTimeline.HeadIndex = halfMoveIndex;

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
		
		public static Dictionary<Piece, Dictionary<(Square, Square), Movement>> CalculateLegalMovesForPosition(
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

		public static List<Movement> UnpackMovementsToList(Dictionary<Piece, Dictionary<(Square, Square), Movement>> possibleMovesPerPiece)
		{
			List<Movement> movements = new List<Movement>();

			foreach (Piece piece in possibleMovesPerPiece.Keys)
			{
				foreach (Movement move in possibleMovesPerPiece[piece].Values)
				{
					if (piece is Pawn)
					{
						if (move is PromotionMove)
						{// generate moves with promotions for each piece type that can be obtained via a promotion
							Side currentSide = piece.Owner;
							PromotionMove promoMoveQueen = new PromotionMove(move.Start, move.End);
							PromotionMove promoMoveBishop = new PromotionMove(move.Start, move.End);
							PromotionMove promoMoveKnight = new PromotionMove(move.Start, move.End);
							PromotionMove promoMoveRook = new PromotionMove(move.Start, move.End);

							promoMoveQueen.SetPromotionPiece(new Queen(currentSide));
							promoMoveBishop.SetPromotionPiece(new Bishop(currentSide));
							promoMoveKnight.SetPromotionPiece(new Knight(currentSide));
							promoMoveRook.SetPromotionPiece(new Rook(currentSide));
							movements.Add(promoMoveQueen);
							movements.Add(promoMoveBishop);
							movements.Add(promoMoveKnight);
							movements.Add(promoMoveRook);
							continue;
						}
					}
					movements.Add(move);
				}
			}

			return movements;
		}
	}
}