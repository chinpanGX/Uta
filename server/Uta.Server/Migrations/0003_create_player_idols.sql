CREATE TABLE player_idols (
    player_id char(26) NOT NULL REFERENCES players (player_id),
    idol_id int NOT NULL,
    training int NOT NULL DEFAULT 0,
    awakening int NOT NULL DEFAULT 0,
    shards int NOT NULL DEFAULT 0,
    created_at timestamptz NOT NULL DEFAULT now(),
    PRIMARY KEY (player_id, idol_id)
);
