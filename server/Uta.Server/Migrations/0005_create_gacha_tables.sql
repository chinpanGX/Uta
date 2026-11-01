CREATE TABLE gacha_exchange_points (
    player_id char(26) NOT NULL REFERENCES players (player_id),
    gacha_id int NOT NULL,
    points int NOT NULL DEFAULT 0 CHECK (points >= 0),
    PRIMARY KEY (player_id, gacha_id)
);

CREATE TABLE gacha_histories (
    history_id char(26) PRIMARY KEY,
    request_id varchar(64) NOT NULL,
    player_id char(26) NOT NULL REFERENCES players (player_id),
    gacha_id int NOT NULL,
    slot int NOT NULL,
    gacha_table_id int NOT NULL,
    item_kind int NOT NULL,
    item_id int NOT NULL,
    outcome int NOT NULL,
    shards int NOT NULL DEFAULT 0,
    support_points int NOT NULL DEFAULT 0,
    created_at timestamptz NOT NULL DEFAULT now()
);
CREATE INDEX ix_gacha_histories_player ON gacha_histories (player_id, history_id DESC);
