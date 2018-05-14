CREATE TABLE __buildmaster_dbschemachanges (
  numeric_release_number bigint not null,
  script_id int not null,
  script_name varchar(50) not null,
  executed_date timestamp not null,
  success_indicator char(1) not null,

  CONSTRAINT __buildmaster_dbschemachangespk
	PRIMARY KEY (numeric_release_number, script_id)
)
;

INSERT INTO __buildmaster_dbschemachanges
	(numeric_release_number, script_id, script_name, executed_date, success_indicator)
VALUES
	(0, 0, 'create table __buildmaster_dbschemachanges', current_timestamp at time zone 'utc', 'Y')
;