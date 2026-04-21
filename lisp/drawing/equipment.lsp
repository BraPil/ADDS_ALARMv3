;;; ADDS Equipment Placement Module
;;; Modernized: credentials from environment (not hardcoded), input sanitization,
;;;             Oracle 19c DSN (ADS_OADB_19C).

(defun C:ADDS-PLACE-PUMP (/ ins-pt tag model conn)
  (setq ins-pt (getpoint "\nPump insertion point: "))
  (setq tag (adds-sanitize-tag (getstring T "\nPump tag: ")))
  (setq model (adds-select-pump-model))
  (adds-insert-equipment-block "PUMP-CENTRIFUGAL" ins-pt tag)
  (setq conn (adds-oadb-connect *ADDS-ORACLE-HOST* *ADDS-ORACLE-PORT* *ADDS-ORACLE-SID*))
  (adds-db-save-equipment tag "PUMP" model ins-pt conn)
  (adds-oadb-disconnect conn)
  (princ (strcat "\nPump " tag " placed successfully."))
)

(defun adds-select-pump-model (/ models)
  (setq models '("P-100A" "P-100B" "P-200" "P-300-VERTICAL"))
  (car models)
)

(defun adds-insert-equipment-block (blk-name ins-pt tag / scale)
  (setq scale *ADDS-DRAWING-SCALE*)
  (command "INSERT" blk-name ins-pt scale scale 0)
)

(defun adds-sanitize-tag (raw / safe c)
  ;; Restrict to [A-Za-z0-9_\-] — reduces SQL injection exposure for OADB calls
  ;; that cannot use bind variables.
  (setq safe "")
  (foreach c (vl-string->list raw)
    (if (or (and (>= c 48) (<= c 57))
            (and (>= c 65) (<= c 90))
            (and (>= c 97) (<= c 122))
            (= c 45) (= c 95))
      (setq safe (strcat safe (chr c)))
    )
  )
  safe
)

(defun adds-db-save-equipment (tag type model location conn / safe-tag safe-type safe-model sql)
  (setq safe-tag   (adds-sanitize-tag tag))
  (setq safe-type  (adds-sanitize-tag type))
  (setq safe-model (adds-sanitize-tag model))
  (setq sql (strcat "INSERT INTO EQUIPMENT(TAG,TYPE,MODEL,LOCATION,CREATED_BY,CREATED_DATE)"
                    " VALUES('" safe-tag "','" safe-type "','" safe-model "','"
                    (vl-princ-to-string location) "','"
                    *ADDS-USER-NAME* "',SYSDATE)"))
  (ads_oadb_execute conn sql)
)

(defun C:ADDS-PLACE-HEAT-EXCHANGER (/ ins-pt tag shell-side tube-side area conn)
  (setq ins-pt (getpoint "\nHeat exchanger location: "))
  (setq tag (adds-sanitize-tag (getstring T "\nHX tag: ")))
  (setq shell-side (adds-sanitize-tag (getstring "\nShell-side fluid: ")))
  (setq tube-side  (adds-sanitize-tag (getstring "\nTube-side fluid: ")))
  (setq area (getreal "\nHeat transfer area (ft2): "))
  (adds-insert-equipment-block "HX-SHELLTUBE" ins-pt tag)
  (setq conn (adds-oadb-connect *ADDS-ORACLE-HOST* *ADDS-ORACLE-PORT* *ADDS-ORACLE-SID*))
  (adds-db-save-hx tag shell-side tube-side area conn)
  (adds-oadb-disconnect conn)
)

(defun adds-db-save-hx (tag shell tube area conn / sql)
  (setq sql (strcat "INSERT INTO HEAT_EXCHANGERS VALUES(SEQ_HX.NEXTVAL,'"
                    tag "','" shell "','" tube "'," (rtos area) ",SYSDATE)"))
  (ads_oadb_execute conn sql)
)

(defun C:ADDS-PLACE-TANK (/ ins-pt tag cap mat conn)
  (setq ins-pt (getpoint "\nTank center: "))
  (setq tag (adds-sanitize-tag (getstring T "\nTank tag: ")))
  (setq cap (getreal "\nCapacity (gallons): "))
  (setq mat (adds-sanitize-tag (getstring "\nMaterial <CS>: ")))
  (if (= mat "") (setq mat "CS"))
  (command "CIRCLE" ins-pt (* (sqrt (/ cap (* 3.14159 *ADDS-DRAWING-SCALE*))) 0.5))
  (adds-draw-tag ins-pt tag)
  (setq conn (adds-oadb-connect *ADDS-ORACLE-HOST* *ADDS-ORACLE-PORT* *ADDS-ORACLE-SID*))
  (adds-db-save-equipment tag "TANK" mat ins-pt conn)
  (adds-oadb-disconnect conn)
)

(defun C:ADDS-EQUIPMENT-REPORT (/ conn rs row)
  (setq conn (adds-oadb-connect *ADDS-ORACLE-HOST* *ADDS-ORACLE-PORT* *ADDS-ORACLE-SID*))
  (setq rs (ads_oadb_query conn
    "SELECT E.TAG, E.TYPE, E.MODEL, E.CREATED_DATE FROM EQUIPMENT E ORDER BY E.TAG"))
  (princ "\n--- EQUIPMENT REPORT ---")
  (while (setq row (ads_oadb_fetchrow rs))
    (princ (strcat "\n" (nth 0 row) "  " (nth 1 row) "  " (nth 2 row)))
  )
  (adds-oadb-disconnect conn)
)

(defun C:ADDS-DELETE-EQUIPMENT (/ tag safe-tag conn)
  (setq tag (getstring T "\nTag to delete: "))
  (setq safe-tag (adds-sanitize-tag tag))
  (if (= (getstring "\nConfirm delete? (Y/N): ") "Y")
    (progn
      (setq conn (adds-oadb-connect *ADDS-ORACLE-HOST* *ADDS-ORACLE-PORT* *ADDS-ORACLE-SID*))
      (ads_oadb_execute conn (strcat "DELETE FROM EQUIPMENT WHERE TAG='" safe-tag "'"))
      (adds-oadb-disconnect conn)
      (princ (strcat "\n" safe-tag " deleted."))
    )
  )
)
