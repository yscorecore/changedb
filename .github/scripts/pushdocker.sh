#!/usr/bin/env bash

DOCKER_IMAGES_FILE="docker-images.yml"

if [ ! -f $DOCKER_IMAGES_FILE ]; then 
    echo "Can not find '$DOCKER_IMAGES_FILE' in root folder, ignore push docker images."
    exit
fi

IMAGES_COUNT=$(docker run --rm -v "${PWD}":/workdir mikefarah/yq yq r "$DOCKER_IMAGES_FILE" --length services)

if [ "${IMAGES_COUNT}" = "0" ]; then
    echo "Can not find any services in '$DOCKER_IMAGES_FILE'."
    exit
fi

build_and_push()
{
  docker-compose -f $DOCKER_IMAGES_FILE build && docker-compose -f $DOCKER_IMAGES_FILE push
}

DOCKER_USERNAME=$1
DOCKER_PASSWORD=$2
echo "${DOCKER_PASSWORD}" | docker login -u ${DOCKER_USERNAME} --password-stdin
build_and_push
docker logout

