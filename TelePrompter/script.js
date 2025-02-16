document.addEventListener("DOMContentLoaded", function () {
    const textInput = document.getElementById("text-input");
    const startBtn = document.getElementById("start-btn");
    const resetBtn = document.getElementById("reset-btn");
    const stopBtn = document.getElementById("stop-btn");
    const startpauseBtn = document.getElementById("startpause-btn");
    const fontSizeSelect = document.getElementById("font-size");
    const scrollSpeed = document.getElementById("scroll-speed");
    const wpmSlider = document.getElementById("wpm");
    const teleprompter = document.getElementById("teleprompter");
    const inputSection = document.getElementById("input-section");
    const controls = document.getElementById("controls");
    
    let index = 0;
    let words = [];
    let currentWordIndex = 0;
    let scrolling;
    let rolling;
    let recognition;
    let running = false

    // Avvia il teleprompter
    startBtn.addEventListener("click", function () {
        const text = textInput.value.trim();
        if (text === "") return alert("Inserisci un testo!");

        words = text.split(" ").map(word => `<span class="word">${word}</span>`);
        teleprompter.innerHTML = words.join(" ");
        
        inputSection.classList.add("hidden");
        controls.classList.remove("hidden");
        teleprompter.classList.remove("hidden");

        // startScrolling();
        // startVoiceRecognition();
    });

    // Ferma il teleprompter
    stopBtn.addEventListener("click", function () {
        clearInterval(scrolling);
        if (recognition) recognition.stop();
        inputSection.classList.remove("hidden");
        controls.classList.add("hidden");
        teleprompter.classList.add("hidden");
    });

    resetBtn.addEventListener("click", function () {
        clearInterval(scrolling);
        clearInterval(rolling);
        if (recognition) recognition.stop();
        index = 0;
        document.querySelectorAll(".word").forEach((word, i) => {
            word.classList.remove("active");
        });
    });

    startpauseBtn.addEventListener("click", function () {
        if(!running){
            highlightWords();
            startVoiceRecognition();
            startScrolling();
            running = true;
            return;
        }
        clearInterval(scrolling);
        clearInterval(rolling);
        if (recognition) recognition.stop();
        running = false;
    });

    // Modifica dimensione del testo
    fontSizeSelect.addEventListener("change", function () {
        teleprompter.style.fontSize = fontSizeSelect.value;
    });

    // Funzione per lo scorrimento automatico
    function startScrolling() {
        clearInterval(scrolling);
        scrolling = setInterval(() => {
            document.documentElement.scrollTop += parseInt(scrollSpeed.value);
        }, 100);
    }

    // Cambia colore in base a WPM
    function highlightWords() {
        const wpm = parseInt(wpmSlider.value);
        const interval = 60000 / wpm; // Tempo per parola
        
        clearInterval(rolling);
        rolling = setInterval(() => {
            if (index < words.length) {
                document.querySelectorAll(".word").forEach((word, i) => {
                    word.classList.toggle("active", i <= index);
                });
                index++;
            }
        }, interval);
    }

    // Riconoscimento vocale per sincronizzazione
    function startVoiceRecognition() {
        if (!("webkitSpeechRecognition" in window)) {
            alert("Il tuo browser non supporta il riconoscimento vocale.");
            return;
        }

        recognition = new webkitSpeechRecognition();
        recognition.lang = "it-IT";
        recognition.continuous = true;

        recognition.onresult = function (event) {
            const transcript = event.results[event.results.length - 1][0].transcript.trim();
            syncTextWithVoice(transcript);
        };

        recognition.start();
    }

    // Sincronizza il testo con la voce
    function syncTextWithVoice(transcript) {
        const wordsArray = words.map(word => word.replace(/<[^>]+>/g, "").toLowerCase());
        const spokenWords = transcript.toLowerCase().split(" ");
        
        let lastMatchedIndex = -1;
        spokenWords.forEach(spokenWord => {
            const index = wordsArray.indexOf(spokenWord, lastMatchedIndex + 1);
            if (index !== -1) lastMatchedIndex = index;
        });

        if (lastMatchedIndex > currentWordIndex) {
            currentWordIndex = lastMatchedIndex;
            document.querySelectorAll(".word").forEach((word, i) => {
                word.classList.toggle("active", i <= currentWordIndex);
            });

            teleprompter.scrollTop = document.querySelectorAll(".word")[currentWordIndex].offsetTop - 50;
        }
    }
});
