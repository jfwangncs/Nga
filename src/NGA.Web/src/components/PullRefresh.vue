<template>
  <div class="pull-refresh-container" ref="containerRef">
    <div
      class="pull-refresh-indicator"
      :style="{
        transform: `translateY(${indicatorPosition}px)`,
        opacity: indicatorOpacity,
      }"
    >
      <div class="indicator-content" :class="{ refreshing: isRefreshing }">
        <svg
          v-if="!isRefreshing"
          class="refresh-icon"
          :class="{ flip: pullDistance >= threshold }"
          viewBox="0 0 24 24"
          width="24"
          height="24"
        >
          <path
            fill="currentColor"
            d="M12 4V1L8 5l4 4V6c3.31 0 6 2.69 6 6 0 1.01-.25 1.97-.7 2.8l1.46 1.46C19.54 15.03 20 13.57 20 12c0-4.42-3.58-8-8-8zm0 14c-3.31 0-6-2.69-6-6 0-1.01.25-1.97.7-2.8L5.24 7.74C4.46 8.97 4 10.43 4 12c0 4.42 3.58 8 8 8v3l4-4-4-4v3z"
          />
        </svg>
        <div v-else class="spinner">
          <div class="spinner-circle"></div>
        </div>
        <span class="indicator-text">{{ indicatorText }}</span>
      </div>
    </div>
    <div
      class="pull-refresh-content"
      :style="{ transform: `translateY(${contentPosition}px)` }"
      @touchstart="handleTouchStart"
      @touchmove="handleTouchMove"
      @touchend="handleTouchEnd"
      @mousedown="handleMouseDown"
    >
      <slot></slot>
    </div>
  </div>
</template>

<script setup>
import { ref, computed } from "vue";

const props = defineProps({
  // 触发刷新的下拉距离阈值（像素）
  threshold: {
    type: Number,
    default: 80,
  },
  // 刷新回调函数
  onRefresh: {
    type: Function,
    required: true,
  },
  // 禁用下拉刷新
  disabled: {
    type: Boolean,
    default: false,
  },
});

const containerRef = ref(null);
const startY = ref(0);
const currentY = ref(0);
const pullDistance = ref(0);
const isRefreshing = ref(false);
const isPulling = ref(false);
const isMouseDown = ref(false);

// 指示器位置
const indicatorPosition = computed(() => {
  if (isRefreshing.value) {
    return props.threshold / 2;
  }
  return Math.min(pullDistance.value - 30, props.threshold / 2);
});

// 指示器透明度
const indicatorOpacity = computed(() => {
  if (isRefreshing.value) {
    return 1;
  }
  return Math.min(pullDistance.value / props.threshold, 1);
});

// 内容位置
const contentPosition = computed(() => {
  if (isRefreshing.value) {
    return props.threshold;
  }
  return pullDistance.value;
});

// 指示器文本
const indicatorText = computed(() => {
  if (isRefreshing.value) {
    return "正在刷新...";
  }
  if (pullDistance.value >= props.threshold) {
    return "释放刷新";
  }
  return "下拉刷新";
});

// 检查是否可以下拉
const canPull = () => {
  if (props.disabled || isRefreshing.value) {
    return false;
  }
  // 检查是否在页面顶部
  const scrollTop = window.pageYOffset || document.documentElement.scrollTop;
  return scrollTop === 0;
};

// 触摸开始
const handleTouchStart = (e) => {
  if (!canPull()) {
    return;
  }
  startY.value = e.touches[0].clientY;
  currentY.value = startY.value;
  isPulling.value = true;
};

// 触摸移动
const handleTouchMove = (e) => {
  if (!isPulling.value || !canPull()) {
    return;
  }

  currentY.value = e.touches[0].clientY;
  const distance = currentY.value - startY.value;

  if (distance > 0) {
    // 阻止默认滚动行为
    e.preventDefault();
    // 添加阻尼效果
    pullDistance.value = Math.pow(distance, 0.8);
  }
};

// 触摸结束
const handleTouchEnd = async () => {
  if (!isPulling.value) {
    return;
  }

  isPulling.value = false;

  if (pullDistance.value >= props.threshold && !isRefreshing.value) {
    isRefreshing.value = true;
    try {
      await props.onRefresh();
    } finally {
      setTimeout(() => {
        isRefreshing.value = false;
        pullDistance.value = 0;
      }, 300);
    }
  } else {
    pullDistance.value = 0;
  }
};

// 鼠标事件（用于桌面端测试）
const handleMouseDown = (e) => {
  if (!canPull()) {
    return;
  }
  isMouseDown.value = true;
  startY.value = e.clientY;
  currentY.value = startY.value;
  isPulling.value = true;

  const handleMouseMove = (e) => {
    if (!isPulling.value || !canPull() || !isMouseDown.value) {
      return;
    }

    currentY.value = e.clientY;
    const distance = currentY.value - startY.value;

    if (distance > 0) {
      e.preventDefault();
      pullDistance.value = Math.pow(distance, 0.8);
    }
  };

  const handleMouseUp = async () => {
    isMouseDown.value = false;
    document.removeEventListener("mousemove", handleMouseMove);
    document.removeEventListener("mouseup", handleMouseUp);

    await handleTouchEnd();
  };

  document.addEventListener("mousemove", handleMouseMove);
  document.addEventListener("mouseup", handleMouseUp);
};
</script>

<style scoped>
.pull-refresh-container {
  position: relative;
  overflow: hidden;
  width: 100%;
  height: 100%;
}

.pull-refresh-indicator {
  position: fixed;
  top: 0;
  left: 0;
  right: 0;
  margin: 0 auto;
  width: fit-content;
  z-index: 999;
  padding: 10px 20px;
  background: rgba(255, 255, 255, 0.95);
  border-radius: 0 0 20px 20px;
  box-shadow: 0 4px 12px rgba(0, 0, 0, 0.1);
  backdrop-filter: blur(10px);
  transition:
    opacity 0.3s,
    transform 0.3s;
}

.indicator-content {
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 10px;
  color: #4a90e2;
  min-width: 140px;
}

.indicator-content.refreshing .spinner {
  animation: spin 1s linear infinite;
}

.refresh-icon {
  transition: transform 0.3s;
  color: #4a90e2;
  flex-shrink: 0;
}

.refresh-icon.flip {
  transform: rotate(180deg);
}

.spinner {
  width: 24px;
  height: 24px;
  position: relative;
  flex-shrink: 0;
}

.spinner-circle {
  width: 24px;
  height: 24px;
  border: 3px solid rgba(74, 144, 226, 0.2);
  border-top-color: #4a90e2;
  border-radius: 50%;
  animation: spin 0.8s linear infinite;
}

@keyframes spin {
  to {
    transform: rotate(360deg);
  }
}

.indicator-text {
  font-size: 14px;
  font-weight: 500;
  white-space: nowrap;
}

.pull-refresh-content {
  transition: transform 0.3s ease-out;
  will-change: transform;
}

/* 移动端优化 */
@media (max-width: 768px) {
  .pull-refresh-indicator {
    max-width: 90vw;
    padding: 8px 16px;
  }

  .indicator-content {
    min-width: 120px;
  }

  .indicator-text {
    font-size: 13px;
  }
}
</style>
