CREATE TABLE IF NOT EXISTS
cron_records
(id SERIAL PRIMARY KEY
, tlid BIGINT NOT NULL
, canvas_id UUID REFERENCES canvases(id) NOT NULL
, ran_at TIMESTAMP NOT NULL DEFAULT NOW ()
)
