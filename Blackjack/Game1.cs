using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
//using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;

using XnaCards;

namespace Blackjack
{
    /// <summary>
    /// This is the main type for your game
    /// </summary>
    public class Game1 : Microsoft.Xna.Framework.Game
    {
        GraphicsDeviceManager graphics;
        SpriteBatch spriteBatch;

        const int WINDOW_WIDTH = 800;
        const int WINDOW_HEIGHT = 600;

        // max valid blackjack score for a hand
        const int MAX_HAND_VALUE = 21;

        // deck and hands
        Deck deck;
        List<Card> dealerHand = new List<Card>();
        List<Card> playerHand = new List<Card>();

        // hand placement
        const int TOP_CARD_OFFSET = 100;
        const int HORIZONTAL_CARD_OFFSET = 150;
        const int VERTICAL_CARD_SPACING = 125;

        // messages
        SpriteFont messageFont;
        const string SCORE_MESSAGE_PREFIX = "Score: ";
        Message playerScoreMessage;
        List<Message> messages = new List<Message>();

        // message placement
        const int SCORE_MESSAGE_TOP_OFFSET = 25;
        const int HORIZONTAL_MESSAGE_OFFSET = HORIZONTAL_CARD_OFFSET;
        Vector2 winnerMessageLocation = new Vector2(WINDOW_WIDTH / 2,
            WINDOW_HEIGHT / 2);

        // menu buttons
        Texture2D quitButtonSprite;
        List<MenuButton> menuButtons = new List<MenuButton>();

        // menu button placement
        const int TOP_MENU_BUTTON_OFFSET = TOP_CARD_OFFSET;
        const int QUIT_MENU_BUTTON_OFFSET = WINDOW_HEIGHT - TOP_CARD_OFFSET;
        const int HORIZONTAL_MENU_BUTTON_OFFSET = WINDOW_WIDTH / 2;
        const int VERTICAL_MENU_BUTTON_SPACING = 125;

        // use to detect hand over when player and dealer didn't hit
        bool playerHit = false;
        bool dealerHit = false;

        // game state tracking
        static GameState currentState = GameState.WaitingForPlayer;

		// mouse state
		MouseState mouse;

        public Game1()
        {
            graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";

            // set resolution and show mouse
			graphics.PreferredBackBufferWidth = WINDOW_WIDTH;
			graphics.PreferredBackBufferHeight = WINDOW_HEIGHT;
			IsMouseVisible = true;

        }

        /// <summary>
        /// Allows the game to perform any initialization it needs to before starting to run.
        /// This is where it can query for any required services and load any non-graphic
        /// related content.  Calling base.Initialize will enumerate through any components
        /// and initialize them as well.
        /// </summary>
        protected override void Initialize()
        {
            // TODO: Add your initialization logic here

            base.Initialize();
        }

        /// <summary>
        /// LoadContent will be called once per game and is the place to load
        /// all of your content.
        /// </summary>
        protected override void LoadContent()
        {
            // Create a new SpriteBatch, which can be used to draw textures.
            spriteBatch = new SpriteBatch(GraphicsDevice);

            // create and shuffle deck
			deck = new Deck(Content, HORIZONTAL_CARD_OFFSET, TOP_CARD_OFFSET);
			deck.Shuffle ();


            // first player card
			playerHand.Add(deck.TakeTopCard());
			playerHand [0].FlipOver ();

            // first dealer card
			dealerHand.Add(deck.TakeTopCard());
//			dealerHand [0].FlipOver ();
			dealerHand [0].X = WINDOW_WIDTH - HORIZONTAL_CARD_OFFSET;

            // second player card
			playerHand.Add(deck.TakeTopCard());
			playerHand [1].FlipOver ();
			playerHand [1].Y = TOP_CARD_OFFSET + VERTICAL_CARD_SPACING;

            // second dealer card
			dealerHand.Add(deck.TakeTopCard());
			dealerHand [1].FlipOver ();
			dealerHand[1].X = WINDOW_WIDTH - HORIZONTAL_CARD_OFFSET;
			dealerHand [1].Y = TOP_CARD_OFFSET + VERTICAL_CARD_SPACING;

            // load sprite font, create message for player score and add to list
            messageFont = Content.Load<SpriteFont>("Arial24");
            playerScoreMessage = new Message(SCORE_MESSAGE_PREFIX + GetBlackjackScore(playerHand).ToString(),
                messageFont,
                new Vector2(HORIZONTAL_MESSAGE_OFFSET, SCORE_MESSAGE_TOP_OFFSET));
            messages.Add(playerScoreMessage);

            // load quit button sprite for later use
            quitButtonSprite = Content.Load<Texture2D>("quitbutton");

            // create hit button and add to list
			menuButtons.Add(new MenuButton (Content.Load<Texture2D> ("hitbutton"),
											new Vector2 (HORIZONTAL_MENU_BUTTON_OFFSET, TOP_MENU_BUTTON_OFFSET),
				GameState.PlayerHitting));

            // create stand button and add to list
			menuButtons.Add(new MenuButton (Content.Load<Texture2D> ("standbutton"),
				new Vector2 (HORIZONTAL_MENU_BUTTON_OFFSET, TOP_MENU_BUTTON_OFFSET+VERTICAL_MENU_BUTTON_SPACING),
				GameState.WaitingForDealer));


        }

        /// <summary>
        /// UnloadContent will be called once per game and is the place to unload
        /// all content.
        /// </summary>
        protected override void UnloadContent()
        {
            // TODO: Unload any non ContentManager content here
        }

        /// <summary>
        /// Allows the game to run logic such as updating the world,
        /// checking for collisions, gathering input, and playing audio.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Update(GameTime gameTime)
        {
            // Allows the game to exit
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed)
                this.Exit();

            // update menu buttons as appropriate
			mouse = Mouse.GetState();
			if (currentState == GameState.WaitingForPlayer || currentState == GameState.DisplayingHandResults) {
				foreach (MenuButton button in menuButtons) {
					button.Update (mouse);
				}
			}

            // game state-specific processing
			switch (currentState) {

			case GameState.PlayerHitting:
				// set the flag
				playerHit = true;
				// add a card to player's hand and set its displaying location
				int phc = playerHand.Count;
				playerHand.Add (deck.TakeTopCard ());
				playerHand [phc].FlipOver ();
				playerHand [phc].Y = TOP_CARD_OFFSET + phc * VERTICAL_CARD_SPACING;
				// update player's score
				messages [0].Text = SCORE_MESSAGE_PREFIX + GetBlackjackScore (playerHand).ToString ();
				// update game state
				currentState = GameState.WaitingForDealer;
				break;
			case GameState.WaitingForDealer:
				// check dealer's score and update game state accordingly
				if (GetBlackjackScore (dealerHand) < 17) {
					currentState = GameState.DealerHitting;
				} else {
					currentState = GameState.CheckingHandOver;
				}
				break;
			case GameState.DealerHitting:
				// set the flag
				dealerHit = true;
				// add a card to dealer's hand and set its displaying location
				int dhc = dealerHand.Count;
				dealerHand.Add (deck.TakeTopCard ());
				dealerHand [dhc].FlipOver ();
				dealerHand [dhc].X = WINDOW_WIDTH - HORIZONTAL_CARD_OFFSET;
				dealerHand [dhc].Y = TOP_CARD_OFFSET + dhc * VERTICAL_CARD_SPACING;
				// update game state
				currentState = GameState.CheckingHandOver;
				break;
			case GameState.CheckingHandOver:
				// check if player or dealer or both had busted OR both stood
				int playerScore = GetBlackjackScore (playerHand);
				int dealerScore = GetBlackjackScore (dealerHand);
				if (playerScore > MAX_HAND_VALUE ||
					dealerScore > MAX_HAND_VALUE ||
					!(playerHit || dealerHit)) {
					// remove hit and stand buttons
					menuButtons.Clear();
					// add quit button
					menuButtons.Add(new MenuButton(quitButtonSprite,
												   new Vector2(WINDOW_WIDTH / 2, QUIT_MENU_BUTTON_OFFSET),
												   GameState.Exiting));
					// flip dealer's first card
					if (!dealerHand [0].FaceUp) {
						dealerHand [0].FlipOver ();
					}
					// show dealer's score
					messages.Add(new Message(SCORE_MESSAGE_PREFIX + dealerScore.ToString(),
											 messageFont,
											 new Vector2(WINDOW_WIDTH - HORIZONTAL_MESSAGE_OFFSET, SCORE_MESSAGE_TOP_OFFSET)));
					// create and show winner message
					String winnerMessage;
					if (playerScore > MAX_HAND_VALUE) {
						if (dealerScore > MAX_HAND_VALUE) {
							winnerMessage = "Both busted. It's a tie!";
						} else {
							winnerMessage = "Player busted. Dealer Won!";
						}
					} else {
						if (dealerScore > MAX_HAND_VALUE) {
							winnerMessage = "Dealer busted. Player Won!";
						} else {
							if (playerScore > dealerScore) {
								winnerMessage = "Player Won!";
							} else if (playerScore < dealerScore) {
								winnerMessage = "Dealer Won!";
							} else {
								winnerMessage = "It's a tie!";
							}
						}
					}
					messages.Add(new Message(winnerMessage, messageFont, winnerMessageLocation));
					// update current state
					currentState = GameState.DisplayingHandResults;

				} else {
					playerHit = false;
					dealerHit = false;
					currentState = GameState.WaitingForPlayer;
				}
				break;
			case GameState.Exiting:
				this.Exit ();
				break;
			default:
				break;
			}

            base.Update(gameTime);
        }

        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.Goldenrod);

            spriteBatch.Begin();

            // draw hands
			foreach (Card card in playerHand) {
				card.Draw (spriteBatch);
			}
			foreach (Card card in dealerHand) {
				card.Draw (spriteBatch);
			}

            // draw messages
			foreach (Message message in messages) {
				message.Draw (spriteBatch);
			}
				
            // draw menu buttons
			foreach (MenuButton button in menuButtons) {
				button.Draw (spriteBatch);
			}

            spriteBatch.End();

            base.Draw(gameTime);
        }

        /// <summary>
        /// Calculates the Blackjack score for the given hand
        /// </summary>
        /// <param name="hand">the hand</param>
        /// <returns>the Blackjack score for the hand</returns>
        private int GetBlackjackScore(List<Card> hand)
        {
            // add up score excluding Aces
            int numAces = 0;
            int score = 0;
            foreach (Card card in hand)
            {
                if (card.Rank != Rank.Ace)
                {
                    score += GetBlackjackCardValue(card);
                }
                else
                {
                    numAces++;
                }
            }

            // if more than one ace, only one should ever be counted as 11
            if (numAces > 1)
            {
                // make all but the first ace count as 1
                score += numAces - 1;
                numAces = 1;
            }

            // if there's an Ace, score it the best way possible
            if (numAces > 0)
            {
                if (score + 11 <= MAX_HAND_VALUE)
                {
                    // counting Ace as 11 doesn't bust
                    score += 11;
                }
                else
                {
                    // count Ace as 1
                    score++;
                }
            }

            return score;
        }

        /// <summary>
        /// Gets the Blackjack value for the given card
        /// </summary>
        /// <param name="card">the card</param>
        /// <returns>the Blackjack value for the card</returns>
        private int GetBlackjackCardValue(Card card)
        {
            switch (card.Rank)
            {
                case Rank.Ace:
                    return 11;
                case Rank.King:
                case Rank.Queen:
                case Rank.Jack:
                case Rank.Ten:
                    return 10;
                case Rank.Nine:
                    return 9;
                case Rank.Eight:
                    return 8;
                case Rank.Seven:
                    return 7;
                case Rank.Six:
                    return 6;
                case Rank.Five:
                    return 5;
                case Rank.Four:
                    return 4;
                case Rank.Three:
                    return 3;
                case Rank.Two:
                    return 2;
                default:
                    return 0;
            }
        }

        /// <summary>
        /// Changes the state of the game
        /// </summary>
        /// <param name="newState">the new game state</param>
        public static void ChangeState(GameState newState)
        {
            currentState = newState;
        }
    }
}
