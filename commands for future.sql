-- delete dublicates

DELETE FROM
    words a
        USING words b
WHERE
    a.id < b.id
    AND a.word = b.word;

-- to add uniqueness

ALTER TABLE words ADD CONSTRAINT word_id UNIQUE (word);