-- プレイヤーは経験値の累計だけを持つ（produce-server.md 4.1）。今の「今のレベルの中の経験値」は捨てる（リリース前）
ALTER TABLE players DROP COLUMN level;
UPDATE players SET exp = 0;
