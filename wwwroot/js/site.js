document.addEventListener("DOMContentLoaded", () => {
    // ── Sidebar Collapse Toggle ──────────────────────────────
    const sidebar = document.getElementById("sidebarPanel");
    const collapseBtn = document.getElementById("sidebarCollapseBtn");

    if (sidebar && collapseBtn) {
        // Restore persisted state
        const savedCollapsed = localStorage.getItem("pos_sidebar_collapsed") === "true";
        if (savedCollapsed) {
            sidebar.classList.add("collapsed");
        }

        collapseBtn.addEventListener("click", () => {
            sidebar.classList.toggle("collapsed");
            localStorage.setItem(
                "pos_sidebar_collapsed",
                sidebar.classList.contains("collapsed")
            );
        });
    }

    // ── Mobile overlay toggle (hamburger in topbar) ──────────
    const mobileToggle = document.getElementById("sidebarToggle");
    if (mobileToggle && sidebar) {
        mobileToggle.addEventListener("click", () => {
            sidebar.classList.toggle("open");
        });
    }

    // ── Toasts ───────────────────────────────────────────────
    document.querySelectorAll(".toast").forEach((toastEl) => {
        const toast = new bootstrap.Toast(toastEl, { delay: 4000 });
        toast.show();
    });

    initializeForecastCharts(document);
    initializeForecastDetailModal();
});

function initializeForecastCharts(root) {
    root.querySelectorAll(".forecast-chart").forEach((canvas, index) => {
        const rawSeries = canvas.dataset.chartSeries;
        if (!rawSeries) {
            return;
        }

        const series = JSON.parse(rawSeries);
        const labels = series.map((point) => point.label ?? point.Label);
        const values = series.map((point) => point.value ?? point.Value);
        const palette = ["#2563eb", "#0f766e", "#f97316", "#7c3aed", "#dc2626", "#0891b2", "#65a30d", "#334155"];
        const type = canvas.dataset.chartType || "bar";

        new Chart(canvas, {
            type,
            data: {
                labels,
                datasets: [{
                    label: canvas.dataset.chartTitle || "Forecast",
                    data: values,
                    borderColor: palette[index % palette.length],
                    backgroundColor: type === "doughnut"
                        ? palette.map((color) => `${color}cc`)
                        : `${palette[index % palette.length]}cc`,
                    borderWidth: 2,
                    borderRadius: type === "bar" ? 10 : 0,
                    tension: 0.35,
                    fill: type === "line"
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: {
                    legend: {
                        display: type === "doughnut"
                    }
                },
                scales: type === "doughnut" ? {} : {
                    x: {
                        grid: {
                            display: false
                        }
                    },
                    y: {
                        beginAtZero: true,
                        ticks: {
                            precision: 0
                        }
                    }
                }
            }
        });
    });
}

function initializeForecastDetailModal() {
    const modal = document.getElementById("forecastDetailModal");
    const container = document.getElementById("forecastDetailContainer");
    if (!modal || !container) {
        return;
    }

    document.querySelectorAll(".forecast-detail-trigger").forEach((button) => {
        button.addEventListener("click", async () => {
            container.innerHTML = "<div class=\"forecast-loading-state\"><div class=\"spinner-border text-primary\" role=\"status\"></div></div>";
            const detailUrl = button.dataset.detailUrl;
            if (!detailUrl) {
                return;
            }

            try {
                const response = await fetch(detailUrl, {
                    headers: {
                        "X-Requested-With": "XMLHttpRequest"
                    }
                });

                if (!response.ok) {
                    throw new Error("Unable to load forecast detail.");
                }

                container.innerHTML = await response.text();
                initializeDetailChart(container);
            } catch (error) {
                container.innerHTML = `<div class="alert alert-danger mb-0">${error.message}</div>`;
            }
        });
    });
}

function initializeDetailChart(root) {
    const chart = root.querySelector("#forecastDetailChart");
    if (!chart) {
        return;
    }

    const labels = (chart.dataset.labels || "").split("|").filter(Boolean);
    const values = (chart.dataset.values || "").split("|").filter(Boolean).map(Number);

    new Chart(chart, {
        type: "line",
        data: {
            labels,
            datasets: [{
                label: "Predicted quantity",
                data: values,
                fill: true,
                borderColor: "#2563eb",
                backgroundColor: "rgba(37, 99, 235, 0.12)",
                tension: 0.35
            }]
        },
        options: {
            responsive: true,
            maintainAspectRatio: false,
            plugins: {
                legend: {
                    display: false
                }
            },
            scales: {
                y: {
                    beginAtZero: true
                }
            }
        }
    });
}
