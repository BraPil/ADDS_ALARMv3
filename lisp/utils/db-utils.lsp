;;; ADDS Database Utilities
;;; Oracle 7/8 OADB interface layer

(defun adds-db-init (/ conn)
  (setq conn (adds-oadb-connect *ADDS-ORACLE-HOST* *ADDS-ORACLE-PORT* *ADDS-ORACLE-SID*))
  (setq *ADDS-DB-CONNECTION* conn)
  (if conn
    (progn
      (setq *ADDS-USER-NAME* (adds-db-get-username conn))
      (princ "\nADDS: Database connected.")
    )
    (progn
      (alert "ADDS: Database connection failed!\nCheck Oracle OADB configuration.")
      nil
    )
  )
)

(defun adds-db-get-username (conn / rs row)
  (setq rs (ads_oadb_query conn "SELECT USER FROM DUAL"))
  (setq row (ads_oadb_fetchrow rs))
  (if row (car row) "UNKNOWN")
)

(defun adds-db-save-vessel (tag ctr rad / sql)
  (setq sql (strcat "INSERT INTO VESSELS(TAG,CTR_X,CTR_Y,RADIUS,CREATED_BY,CREATED_DATE)"
                    " VALUES('" tag "'," (rtos (car ctr)) "," (rtos (cadr ctr))
                    "," (rtos rad) ",'" *ADDS-USER-NAME* "',SYSDATE)"))
  (if *ADDS-DB-CONNECTION*
    (ads_oadb_execute *ADDS-DB-CONNECTION* sql)
    (adds-db-queue-write sql)
  )
)

(defun adds-db-save-instrument (tag type pt / sql)
  (setq sql (strcat "INSERT INTO INSTRUMENTS(TAG,INSTR_TYPE,POS_X,POS_Y,CREATED_BY,CREATED_DATE)"
                    " VALUES('" tag "','" type "'," (rtos (car pt)) ","
                    (rtos (cadr pt)) ",'" *ADDS-USER-NAME* "',SYSDATE)"))
  (if *ADDS-DB-CONNECTION*
    (ads_oadb_execute *ADDS-DB-CONNECTION* sql)
    (adds-db-queue-write sql)
  )
)

(defun adds-db-queue-write (sql / fh)
  (setq fh (open "C:\\ADDS\\db_queue.sql" "a"))
  (write-line sql fh)
  (close fh)
)

(defun adds-db-flush-queue (/ fh line conn count)
  (setq conn (adds-oadb-connect *ADDS-ORACLE-HOST* *ADDS-ORACLE-PORT* *ADDS-ORACLE-SID*))
  (setq fh (open "C:\\ADDS\\db_queue.sql" "r"))
  (setq count 0)
  (while (setq line (read-line fh))
    (ads_oadb_execute conn line)
    (setq count (1+ count))
  )
  (close fh)
  (adds-oadb-disconnect conn)
  (vl-file-delete "C:\\ADDS\\db_queue.sql")
  (princ (strcat "\nFlushed " (itoa count) " queued writes."))
)

(defun adds-db-query-all (table / conn rs rows row)
  (setq conn (adds-oadb-connect *ADDS-ORACLE-HOST* *ADDS-ORACLE-PORT* *ADDS-ORACLE-SID*))
  (setq rs (ads_oadb_query conn (strcat "SELECT * FROM " table)))
  (setq rows '())
  (while (setq row (ads_oadb_fetchrow rs))
    (setq rows (append rows (list row)))
  )
  (adds-oadb-disconnect conn)
  rows
)

(defun adds-db-execute-raw (sql / conn)
  (setq conn (adds-oadb-connect *ADDS-ORACLE-HOST* *ADDS-ORACLE-PORT* *ADDS-ORACLE-SID*))
  (ads_oadb_execute conn sql)
  (adds-oadb-disconnect conn)
)
