create table users (
id serial,
userid int not null unique,
inGame bool not null DEFAULT FALSE,
violation int not null default 0,
words int[],
answers varchar(30)[] default NULL
);
create table words (
id serial,
word varchar(30),
definition text
);
create table admins (
id serial,
userid int not null references users(userid)
);