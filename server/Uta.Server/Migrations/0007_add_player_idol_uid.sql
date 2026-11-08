-- 既存の行に ULID の形（Crockford の base32。時刻 10 文字 + 乱数 16 文字）の値を入れるための一時関数
CREATE FUNCTION pg_temp.uta_ulid(ts timestamptz) RETURNS text
LANGUAGE plpgsql AS $$
DECLARE
    alphabet constant text := '0123456789ABCDEFGHJKMNPQRSTVWXYZ';
    ms bigint := floor(extract(epoch FROM ts) * 1000)::bigint;
    result text := '';
    i int;
BEGIN
    FOR i IN 1..10 LOOP
        result := substr(alphabet, (ms % 32)::int + 1, 1) || result;
        ms := ms / 32;
    END LOOP;
    FOR i IN 1..16 LOOP
        result := result || substr(alphabet, floor(random() * 32)::int + 1, 1);
    END LOOP;
    RETURN result;
END;
$$;

ALTER TABLE player_idols ADD COLUMN idol_uid char(26);
UPDATE player_idols SET idol_uid = pg_temp.uta_ulid(created_at);
ALTER TABLE player_idols ALTER COLUMN idol_uid SET NOT NULL;
ALTER TABLE player_idols ADD CONSTRAINT player_idols_idol_uid_key UNIQUE (idol_uid);

DROP FUNCTION pg_temp.uta_ulid(timestamptz);
