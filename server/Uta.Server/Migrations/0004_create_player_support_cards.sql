CREATE TABLE player_support_cards (
    support_card_uid char(26) PRIMARY KEY,
    player_id char(26) NOT NULL REFERENCES players (player_id),
    card_id int NOT NULL,
    level int NOT NULL DEFAULT 1,
    limit_break int NOT NULL DEFAULT 0,
    acquired_at timestamptz NOT NULL DEFAULT now()
);
CREATE INDEX ix_player_support_cards_player_card ON player_support_cards (player_id, card_id);
