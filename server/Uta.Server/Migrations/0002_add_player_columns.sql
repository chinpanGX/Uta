ALTER TABLE players
    ADD COLUMN level int NOT NULL DEFAULT 1,
    ADD COLUMN exp int NOT NULL DEFAULT 0,
    ADD COLUMN gold int NOT NULL DEFAULT 0,
    ADD COLUMN support_points int NOT NULL DEFAULT 0,
    ADD COLUMN gacha_currency int NOT NULL DEFAULT 0;
