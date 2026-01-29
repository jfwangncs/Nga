<template>
  <div class="user-avatar" :class="sizeClass" :title="username">
    <img
      v-if="avatar"
      :src="avatar"
      :alt="username"
      referrerpolicy="no-referrer"
      @error="handleImageError"
      class="avatar-img"
    />
    <div v-else class="avatar-placeholder">
      <span class="avatar-text">{{ avatarText }}</span>
    </div>
  </div>
</template>

<script setup>
import { ref, computed } from "vue";

const props = defineProps({
  avatar: {
    type: String,
    default: "",
  },
  username: {
    type: String,
    default: "用户",
  },
  size: {
    type: String,
    default: "medium", // small, medium, large
    validator: (value) => ["small", "medium", "large"].includes(value),
  },
});

const imageError = ref(false);

const sizeClass = computed(() => `avatar-${props.size}`);

const avatarText = computed(() => {
  if (!props.username) return "?";
  // 取用户名第一个字符
  return props.username.charAt(0).toUpperCase();
});

const handleImageError = () => {
  imageError.value = true;
};
</script>

<style scoped>
.user-avatar {
  position: relative;
  border-radius: 50%;
  overflow: hidden;
  background: linear-gradient(135deg, #5dade2 0%, #3498db 100%);
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.1);
  transition: all 0.3s ease;
  flex-shrink: 0;
}

.user-avatar:hover {
  transform: scale(1.05);
  box-shadow: 0 4px 12px rgba(0, 0, 0, 0.15);
}

/* 尺寸变体 */
.avatar-small {
  width: 32px;
  height: 32px;
}

.avatar-medium {
  width: 40px;
  height: 40px;
}

.avatar-large {
  width: 56px;
  height: 56px;
}

.avatar-img {
  width: 100%;
  height: 100%;
  object-fit: cover;
  display: block;
}

.avatar-placeholder {
  width: 100%;
  height: 100%;
  display: flex;
  align-items: center;
  justify-content: center;
  background: linear-gradient(135deg, #5dade2 0%, #3498db 100%);
}

.avatar-text {
  color: #ffffff;
  font-weight: 600;
  user-select: none;
}

.avatar-small .avatar-text {
  font-size: 14px;
}

.avatar-medium .avatar-text {
  font-size: 16px;
}

.avatar-large .avatar-text {
  font-size: 24px;
}
</style>
