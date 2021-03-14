;;;; -*-  Mode: Lisp -*-
;;;; lmc.lisp

;;;; Pastori Matteo 829863
;;;; Moscatelli Andrea 833139


;;; Ritorna un nuovo stato dopo aver eseguito
;;; lo stato passatogli.

(defun one-instruction (l)
  (let ((stato (nth 0 l))
        (acc (nth 2 l))
        (pc (nth 4 l))
        (mem (nth 6 l))
        (in (nth 8 l))
        (out (nth 10 l))
        (flag (nth 12 l)))

  ;; Controlli.
  
  (if (string-equal stato 'state) 
      (if (and (>= acc 0) (<= acc 999))
          (if (and (>= pc 0) (<= pc 99))
              (if (= (length mem) 100)
                  (if (control-num mem)
                      (if (control-num in)
                          (if (control-num out)
                              (let((valore-memoria (nth pc mem))) 
                                (op 
                                 valore-memoria 
                                 mem 
                                 acc 
                                 pc 
                                 in 
                                 out 
                                 flag)) 
                            nil)
                        nil)
                    nil)
                nil)
            nil)
        nil)
    (crea-l 'halted-state acc pc mem in out flag))))


;;; Esegue l'operazione corrispondente al valore, 
;;; ritornando la lista del nuovo stato.

(defun op (x mem acc pc in out flag)
  (cond ((and (>= x 0) (<= x 99)) 
         (crea-l 'halted-state acc pc mem in out flag)) 
        ((and (>= x 100) (<= x 199)) 
         (cond ((>= (add x acc mem) 1000) 
                (let ((risultato (mod (add x acc mem) 1000))) 
                  (crea-l 
                   'state 
                   risultato 
                   (mod (+ pc 1) 100) 
                   mem 
                   in 
                   out 
                   'flag)))
               ((< (add x acc mem) 1000) 
                (crea-l 
                 'state 
                 (add x acc mem)
                 (mod (+ pc 1) 100) 
                 mem 
                 in 
                 out 
                 'noflag))))
        ((and (>= x 200) (<= x 299)) 
         (cond ((< (sub x acc mem) 0) 
                (let ((risultato (mod (sub x acc mem) 1000)))
                  (crea-l 
                   'state 
                   risultato 
                   (mod (+ pc 1) 100) 
                   mem 
                   in 
                   out 
                   'flag)))
               ((>= (sub x acc mem) 0) 
                (crea-l 
                 'state 
                 (sub x acc mem) 
                 (mod (+ pc 1) 100) 
                 mem 
                 in 
                 out 
                 'noflag))))
        ((and (>= x 300) (<= x 399)) 
         (crea-l 
          'state 
          acc 
          (mod (+ pc 1) 100) 
          (store x mem acc) 
          in 
          out 
          flag))
        ((and (>= x 400) (<= x 499)) 
         nil)
        ((and (>= x 500) (<= x 599)) 
         (crea-l 
          'state 
          (load-function x mem) 
          (mod (+ pc 1) 100) 
          mem 
          in 
          out 
          flag))
        ((and (>= x 600) (<= x 699)) 
         (crea-l 
          'state 
          acc 
          (branch x) 
          mem 
          in 
          out 
          flag))
        ((and (>= x 700) (<= x 799)) 
         (crea-l 
          'state 
          acc 
          (branchz x pc acc flag) 
          mem 
          in 
          out 
          flag))
        ((and (>= x 800) (<= x 899)) 
         (crea-l 
          'state 
          acc 
          (branchp x pc flag) 
          mem 
          in 
          out 
          flag))
        ((= x 901) 
         (if (numberp (input in))
             (crea-l 
              'state 
              (input in) 
              (mod (+ pc 1) 100) 
              mem 
              (rest in) 
              out 
              flag) 
           nil))
        ((= x 902) 
         (crea-l 
          'state 
          acc 
          (mod (+ pc 1) 100) 
          mem 
          in 
          (output acc out) 
          flag))))


;;; Crea una lista con i valori che gli vengono passati in input.

(defun crea-l (state acc pc mem in out flag)
  (list state :ACC acc :PC pc :MEM mem :IN in :OUT out :FLAG flag))


;;; ADD.

(defun add (x acc memoria)
  (let ((cella-memoria (nth (mod x 100) memoria)))
    (+ cella-memoria acc)))


;;; SUB.

(defun sub (x acc memoria)
  (let ((cella-memoria (nth (mod x 100) memoria)))
    (- acc cella-memoria)))


;;; STORE.

(defun store (x mem acc)
  (let ((valore (mod x 100)))
    (setf (nth valore mem) acc)
    mem))


;;; LOAD.

(defun load-function (x mem)
  (let ((valore (mod x 100)))
    (nth valore mem)))


;;; BRANCH.

(defun branch (x)
  (mod x 100))


;;; BRANCH IF ZERO.

(defun branchz (x pc acc flag)
  (if (= acc 0)
      (if (string-equal flag 'noflag)
          (mod x 100)
        (mod (+ pc 1) 100))
    (mod (+ pc 1) 100)))


;;; BRANCH IF POSITIVE.

(defun branchp (x pc flag)
  (if (string-equal flag 'noflag)
      (mod x 100)
    (mod (+ pc 1) 100)))


;;; INPUT.

(defun input (in)
  (if (= (length in) 0)
      nil
    (first in)))


;;; OUTPUT.

(defun output (acc out)
  (append out (list acc)))


;;; Esegue one-instruction fino a quando non trova uno stato
;;; uguale a "helted-state", in questo caso ritorna la lista 
;;; di "Output".

(defun execution-loop (x)
  (let ((lista (one-instruction x)))
    (cond ((string-equal (nth 0 lista) 'state) (execution-loop lista))
          ((not (string-equal (nth 0 lista) 'state)) (nth 10 lista)))))


;;; Esegue un controllo sulla lista in input, 
;;; verificando che tutti i numeri della lista 
;;; sia compresi tra 0 e 999 compresi.

(defun control-num (x)
  (if (= (length x) 0) 
      T 
    (if (and (>= (first x) 0) (< (first x) 1000)) 
        (control-num (rest x)) nil) 
    ))


;;; Esegue LMC-LOAD e il risultato lo passa come memoria iniziale
;;; a execution-loop.

(defun lmc-run (filename input)
  (if 
      (and 
       (controllo-valori input 0)
       (controllo-valori (lmc-load filename) 0))
      (execution-loop 
       (list 
        'state 
        :ACC 0 
        :PC 0 
        :MEM (lmc-load filename) 
        :IN input 
        :OUT '() 
        :FLAG 'noflag))))


;;; Ritorna la memoria iniziale.

(defun lmc-load (x)
  (let 
      ((valori-etichette 
        (val-etichette 
         (doppia-l 
          (togli-commenti 
           (get-file x) 
           '() 
           0) 
          '() 
          0) 
         (etichette 
          (doppia-l 
           (togli-commenti 
            (get-file x) 
            '() 
            0) 
           '() 
           0) 
          '() 
          0) 
         '() 
         0))
        (l-etichette 
         (etichette 
          (doppia-l 
           (togli-commenti 
            (get-file x) 
            '() 
            0) 
           '() 
           0) 
          '() 
          0))
        (lista3 
         (cancella-eti 
          (elimina-stringhe-vuote 
           (doppia-l 
            (togli-commenti 
             (get-file x) 
             '() 
             0) 
            '() 
            0) 
           '() 
           0) 
          '() 
          0)))
    (if (controllo-etichette l-etichette 0)
        (zero-aggiungi 
         (val-mem 
          (etichette 
           (doppia-l 
            (togli-commenti 
             (get-file x) 
             '() 
             0) 
            '() 
            0) 
           '() 
           0) 
          valori-etichette 
          lista3 
          '() 
          0))
      nil)))


;;; Dal percorso del file passatogli ritorna una lista
;;; dove ogni elemento è una riga del file, dividendo la
;;; stringa ricevuta dalla funzione "read-file".

(defun get-file (x)
  (lispworks:split-sequence "
" (read-file x)))


;;; Ritorna il contenuto del file come una stringa unica.

(defun read-file (filename)
  (with-open-file (sfile filename)
    (with-output-to-string (sstring)
      (recursive sfile sstring)
      sstring)))


;;; Legge in modo ricorsivo il file.

(defun recursive (in out)
  (let ((char (read-char in nil)))
    (unless (null char)
      (format out "~c" char)
      (recursive in out))))


;;; Elimina dalla lista in input tutti i commenti,
;;; ovvero tutto cio' che c'e' dopo "//" essi compresi.

(defun togli-commenti (x app contatore)
  (cond ((< contatore (length x)) (let 
                                      ((posizione 
                                        (delete-l (nth contatore x) 0))
                                       (new-c (+ 1 contatore)))
                                    (togli-commenti x 
                                                    (append 
                                                     app 
                                                     (list posizione)) 
                                                    new-c)))
        ((= contatore (length x)) 
         (delete-str (delete-space app '() 0) '() 0))))


;;; Elimina dalla stringa in input il commemento, 
;;; ovvero tutto cio' che c'e' dopo "//" essi compresi.

(defun delete-l (stringa cont)
  (cond ((= (length stringa) cont) stringa)
        ((not (and 
               (char-equal (char stringa cont) #\/) 
               (char-equal (char stringa (+ 1 cont))))) 
         (delete-l stringa (+ 1 cont)))
        ((and 
          (char-equal (char stringa cont) #\/) 
          (not (char-equal (char stringa (+ 1 cont)) #\/))) 
         nil)
        ((and 
          (char-equal (char stringa cont) #\/) 
          (char-equal (char stringa (+ 1 cont)) #\/)) 
         (subseq stringa 0 cont))))
  

;;; Funzione che se trova un elemento della lista in input 
;;; uguale a NIL restitusce NIL, altrimenti la lista in input.

(defun controllo-nil (x c)
  (cond ((= c (length x)) x)
        ((< c (length x)) (if 
                              (numberp (nth c x)) 
                              (controllo-nil x (+ 1 c))
                            nil))))


;;; Funzione che se trova un elemento della lista di input
;;; uguale a NIL restituisce NIL, altrimenti T.

(defun controllo-nil-true (x c)
  (cond ((= c (length x)) t)
        ((< c (length x)) (if (numberp (nth c x)) 
                              (controllo-nil-true x (+ 1 c))
                            nil))))


;;; Funzione che cancella dalla lista in input gli
;;; elementi uguali a "".

(defun delete-str (x app c)
  (if (< c (length x)) 
      (if (or (string-equal (nth c x) "") (string-equal (nth c x) "nil"))
          (delete-str x app (+ c 1)) 
        (delete-str x (append app (list (nth c x))) (+ c 1)))
    app))


;;; Per ogni elemento della lista in input, cancella 
;;; gli spazi iniziali e quelli finali.

(defun delete-space (x app c)
  (if (< c (length x)) 
      (delete-space x 
                    (append app 
                            (list 
                             (string-trim '(#\Space #\Tab #\Newline) 
                                          (nth c x)))) (+ 1 c)) app))


;;; Elimina dalla lista in input tutte le strighe vuote.

(defun elimina-stringhe-vuote (x app c)
  (cond ((= c (length x)) app)
        ((< c (length x)) 
         (elimina-stringhe-vuote x 
                                 (append app 
                                         (list 
                                          (delete-str (nth c x) '() 0))) 
                                 (+ 1 c)))))


;;; Controllo che la memoria non abbia più di 100 elementi,
;;; se ne ha meno aggiunge tanti zeri quanti ne servono per
;;; arrivare a 100 elementi.

(defun zero-aggiungi (x)
  (if (> (length x) 100)
      nil
    (cond ((= (length x) 100) x)
          ((< (length x) 100) 
           (append x (make-list (- 100 (length x)) :initial-element 0))))))


;;; Controlla che tutti gli elementi della lista delle etichette
;;; in input (x) siano stringhe e non numeri.

(defun controllo-etichette (x c)
  (cond ((= c (length x)) t)
        ((< c (length x)) 
         (if 
             (not (numberp (parse-integer (nth c x) :junk-allowed t))) 
             (controllo-etichette x (+ 1 c))
           nil))))


;;; Trasforma la lista in input in una lista di liste,
;;; ovvero ogni elemento della lista diventa una lista a sua volta.

(defun doppia-l (x app c)
  (cond ((= c (length x)) app)
        ((< c (length x)) 
         (doppia-l x 
                   (append app 
                           (list 
                            (lispworks:split-sequence " " (nth c x)))) 
                   (+ 1 c)))))


;;; Crea una lista con tutte le etichette.

(defun etichette (x app c)
  (cond ((= c (length x)) app)
        ((< c (length x)) 
         (let ((istruzione (nth 0 (nth c x))))
           (cond ((string-equal istruzione "add") (etichette x app (+ 1 c)))
                 ((string-equal istruzione "sub") (etichette x app (+ 1 c)))
                 ((string-equal istruzione "sta") (etichette x app (+ 1 c)))
                 ((string-equal istruzione "lda") (etichette x app (+ 1 c)))
                 ((string-equal istruzione "bra") (etichette x app (+ 1 c)))
                 ((string-equal istruzione "brz") (etichette x app (+ 1 c)))
                 ((string-equal istruzione "brp") (etichette x app (+ 1 c)))
                 ((string-equal istruzione "inp") (etichette x app (+ 1 c)))
                 ((string-equal istruzione "out") (etichette x app (+ 1 c)))
                 ((string-equal istruzione "hlt") (etichette x app (+ 1 c)))
                 ((string-equal istruzione "dat") (etichette x app (+ 1 c)))
                 (t (etichette x (append app (list istruzione)) (+ 1 c))))))))


;;; Associa alla etichetta passata in ingresso il corrispettivo valore.

(defun val-string (str l c)
  (cond ((string-equal str (nth 0 (nth c l))) c)
        (t (val-string str l (+ 1 c)))))


;;; Crea una lista con i valori delle etichette.

(defun val-etichette (x l-eti app c)
  (cond ((= c (length l-eti)) app)
        ((< c (length l-eti)) 
         (val-etichette x l-eti 
                        (append app 
                                (list 
                                 (val-string (nth c l-eti) x 0))) (+ 1 c)))))


;;; Cancella le etichette iniziali dalla lista in input.

(defun cancella-eti (x app c)
  (cond ((= c (length x)) app)
        ((< c (length x)) 
         (let ((istruzione (nth 0 (nth c x))))
           (cond ((string-equal istruzione "add") 
                  (cancella-eti x (append app (list (nth c x))) (+ 1 c)))
                 ((string-equal istruzione "sub") 
                  (cancella-eti x (append app (list (nth c x))) (+ 1 c)))
                 ((string-equal istruzione "sta") 
                  (cancella-eti x (append app (list (nth c x))) (+ 1 c)))
                 ((string-equal istruzione "lda") 
                  (cancella-eti x (append app (list (nth c x))) (+ 1 c)))
                 ((string-equal istruzione "bra") 
                  (cancella-eti x (append app (list (nth c x))) (+ 1 c)))
                 ((string-equal istruzione "brz") 
                  (cancella-eti x (append app (list (nth c x))) (+ 1 c)))
                 ((string-equal istruzione "brp") 
                  (cancella-eti x (append app (list (nth c x))) (+ 1 c)))
                 ((string-equal istruzione "inp") 
                  (cancella-eti x (append app (list (nth c x))) (+ 1 c)))
                 ((string-equal istruzione "out") 
                  (cancella-eti x (append app (list (nth c x))) (+ 1 c)))
                 ((string-equal istruzione "hlt") 
                  (cancella-eti x (append app (list (nth c x))) (+ 1 c)))
                 ((string-equal istruzione "dat") 
                  (cancella-eti x (append app (list (nth c x))) (+ 1 c)))
                 (t 
                  (cancella-eti x 
                                (append app (list (rest (nth c x)))) 
                                (+ 1 c))))))))


;;; Funzione che ha in input la lista delle etichette, 
;;; la lista dei valori delle etichette, la lista della memoria
;;; e richiama ricorsivamente la funzione VAL-ISTRUZIONE.

(defun val-mem (le lve x app c)
  (cond ((= c (length x)) 
         app)
        ((< c (length x)) 
         (val-mem le lve x 
                  (append app (list (val-istruzione (nth c x) le lve))) 
                  (+ 1 c)))))


;;; Funzione che restituisce T se la stringa è un numero, NIL se non lo e'.

(defun stringa-numero (string)
  (let ((*read-eval* nil))
    (ignore-errors (numberp (read-from-string string)))))


;;; Funzione che restituisce il valore della riga del file assembly.

(defun val-istruzione (x le lve)
   (cond ((= 1 (length x)) 
          (cond ((string-equal "inp" (nth 0 x)) 901)
                ((string-equal "out" (nth 0 x)) 902)
                ((string-equal "hlt" (nth 0 x)) 0)
                ((string-equal "dat" (nth 0 x)) 0)))
         ((= 2 (length x)) 
          (cond ((string-equal "add" (nth 0 x)) 
                 (cond ((stringa-numero (nth 1 x)) 
                        (+ 100 (parse-integer (nth 1 x))))
                       (t (+ 100 (search-etichetta (nth 1 x) le lve 0)))))
                ((string-equal "sub" (nth 0 x)) 
                 (cond ((stringa-numero (nth 1 x)) 
                        (+ 200 (parse-integer (nth 1 x))))
                       (t (+ 200 (search-etichetta (nth 1 x) le lve 0)))))
                ((string-equal "sta" (nth 0 x)) 
                 (cond ((stringa-numero (nth 1 x)) 
                        (+ 300 (parse-integer (nth 1 x))))
                       (t (+ 300 (search-etichetta (nth 1 x) le lve 0)))))
                ((string-equal "lda" (nth 0 x)) 
                 (cond ((stringa-numero (nth 1 x)) 
                        (+ 500 (parse-integer (nth 1 x))))
                       (t (+ 500 (search-etichetta (nth 1 x) le lve 0)))))
                ((string-equal "bra" (nth 0 x)) 
                 (cond ((stringa-numero (nth 1 x)) 
                        (+ 600 (parse-integer (nth 1 x))))
                       (t (+ 600 (search-etichetta (nth 1 x) le lve 0)))))
                ((string-equal "brz" (nth 0 x)) 
                 (cond ((stringa-numero (nth 1 x)) 
                        (+ 700 (parse-integer (nth 1 x))))
                       (t (+ 700 (search-etichetta (nth 1 x) le lve 0)))))
                ((string-equal "brp" (nth 0 x)) 
                 (cond ((stringa-numero (nth 1 x)) 
                        (+ 800 (parse-integer (nth 1 x))))
                       (t (+ 800 (search-etichetta (nth 1 x) le lve 0)))))
                ((string-equal "dat" (nth 0 x)) 
                 (cond ((stringa-numero (nth 1 x)) 
                        (parse-integer (nth 1 x)))
                       (t nil)))
                (t nil))) 
         ((> 2 (length x)) nil)))


;;; Funzione che restituisce il valore dell'etichetta passatagli.

(defun search-etichetta (x le lve c)
  (cond ((string-equal x (nth c le)) (nth c lve))
        (t (search-etichetta x le lve (+ 1 c)))))


;;; Controlla che tutti gli elementi della lista passatagli
;;; siano tra 0 e 999 compresi.

(defun controllo-valori (x c)
  (cond ((= c (length x)) t)
        ((< c (length x)) (if 
                              (and (>= (nth c x) 0) (<= (nth c x) 999)) 
                              (controllo-valori x (+ 1 c))
                            nil))))


;;; end of file -- lmc.lisp