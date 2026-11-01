CREATE TABLE players (
    player_id char(26) PRIMARY KEY,
    created_at timestamptz NOT NULL DEFAULT now()
);
