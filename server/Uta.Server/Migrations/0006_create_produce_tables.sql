CREATE TABLE produce_sessions (
    session_id char(26) PRIMARY KEY,
    player_id char(26) NOT NULL REFERENCES players (player_id),
    status int NOT NULL,
    week int NOT NULL,
    turn int NOT NULL,
    app_version int NOT NULL,
    state_version int NOT NULL,
    rule_version int NOT NULL,
    version int NOT NULL DEFAULT 0,
    state bytea NOT NULL,
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now()
);
CREATE UNIQUE INDEX ux_produce_sessions_active ON produce_sessions (player_id) WHERE status = 0;

CREATE TABLE produce_results (
    session_id char(26) PRIMARY KEY REFERENCES produce_sessions (session_id),
    player_id char(26) NOT NULL REFERENCES players (player_id),
    ending int NOT NULL,
    exam_outcome int NOT NULL,
    evaluation_value int NOT NULL,
    produce_rank int NOT NULL,
    final_score int NOT NULL,
    final_rank int NOT NULL,
    gacha_currency int NOT NULL,
    gold int NOT NULL,
    support_points int NOT NULL,
    exp int NOT NULL,
    created_at timestamptz NOT NULL DEFAULT now()
);
CREATE INDEX ix_produce_results_player ON produce_results (player_id, created_at DESC);
